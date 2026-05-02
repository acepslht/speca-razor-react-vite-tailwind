import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';
import { glob } from 'glob';
import path from 'path';
import child_process from 'child_process';
import fs from 'fs';

function normalize(p) {
    return p.replace(/\\/g, '/');
}

function getEntryPoints(pattern: any, regex: any, keyBuilder: any) {
    const files = glob.sync(pattern, { ignore: ['**/node_modules/**'] });
    const entries = files.map((filePath) => {
        const normalizedPath = normalize(filePath);
        const match = normalizedPath.match(regex);
        if (!match && keyBuilder) return null;
        const key = keyBuilder ? keyBuilder(match) : normalizedPath;
        return [key, path.resolve(__dirname, filePath)];
    }).filter(entry => entry !== null);

    return Object.fromEntries(entries);
}

const getThemes = getEntryPoints(
    '{Apps,Libs}/**/Assets/Themes/**/_*.{jsx,tsx,vue,js,ts,css,scss}',
    /(Apps|Libs)\/([^/]+)\/Assets\/Themes\/.*?([^/]+)\/(?:app\/)?(?:([^/]+)\/)?_([^/.]+)\.(?:jsx|tsx|vue|js|ts|css|scss)$/i,
    (m) => {
        if (!m) return null;

        //const category = m[1].toLowerCase();
        //const project = m[2].toLowerCase().replace(/\./g, '-');
        const themeFolder = m[3].toLowerCase();
        const folderCategory = m[4] ? m[4].toLowerCase() : ''; // Menangkap apa pun: widgets, pages, layouts, components, dll
        const fileName = m[5].toLowerCase();    // Nama file asli
        const categoryPath = folderCategory ? `${folderCategory}/` : '';

        const isMainFile = fileName === themeFolder ||
            fileName === 'index' ||
            fileName === 'style' ||
            fileName === 'theme'

        if (isMainFile) {
            return `themes/${themeFolder}/${folderCategory}`.replace(/\/$/, '');
        }

        return `themes/${themeFolder}/${categoryPath}${fileName}`;
    }
);

const getEntries = getEntryPoints(
    '{Apps,Libs}/**/Assets/Entries/**/*.{jsx,tsx,vue,js,ts}',
    /(Apps|Libs)\/([^/]+)\/Assets\/Entries\/(?:(.*)\/)?([^/.]+)\.(?:jsx|tsx|vue|js|ts)$/i,
    (m) => {
        if (!m) return null;
        const category = m[1].toLowerCase(); // Apps atau Libs
        const project = m[2].toLowerCase().replace(/\./g, '-'); // Contoh: portal
        const subPath = m[3] ? m[3].toLowerCase() : ''; // Sub-folder di dalam Entries
        const fileName = m[4].toLowerCase(); // Nama file
        return `${category}/${project}/${subPath}${fileName}`;
    }
);

const getVendors = getEntryPoints(
    '{Apps,Libs}/**/Assets/Vendors/**/_*.{jsx,tsx,vue,js,ts,css,scss}',
    /(Apps|Libs)\/([^/]+)\/Assets\/Vendors\/.*?([^/]+)\/(?:app\/)?(?:([^/]+)\/)?_([^/.]+)\.(?:jsx|tsx|vue|js|ts|css|scss)$/i,
    (m) => {
        if (!m) return null;
        //const category = m[1].toLowerCase(); // Apps atau Libs
        //const project = m[2].toLowerCase().replace(/\./g, '-'); // Contoh: portal atau ui
        const vendorFolder = m[3].toLowerCase(); // Nama folder vendor
        const folderCategory = m[4] ? m[4].toLowerCase() : ''; // Menangkap apa pun: widgets, pages, layouts, components, dll
        const fileName = m[5].toLowerCase();    // Nama file asli
        const categoryPath = folderCategory ? `${folderCategory}/` : '';

        const isMainFile = fileName === vendorFolder ||
            fileName === 'index' ||
            fileName === 'style' ||
            fileName === 'theme'

        if (isMainFile) {
            return `vendors/${vendorFolder}/${folderCategory}`.replace(/\/$/, '');
        }

        return `vendors/${vendorFolder}/${categoryPath}${fileName}`;
    }
);

const entryPoint = {
    ...getThemes,
    ...getEntries,
    ...getVendors,
    'style': path.resolve(__dirname, 'Libs', 'UI', 'Assets', 'Styles', 'styles.css'),
}

const webAppPath = path.join(__dirname, 'Apps', 'Portal');
let appsettings;
try {
    appsettings = require(path.join(webAppPath,"./appsettings.json"));
} catch (error) {
    console.error("Error: appsettings.json tidak ditemukan atau tidak valid.");
    console.error("Pastikan file ada dan terformat dengan benar.");
    process.exit(1);
}

const config = appsettings.Application;
const certPath = path.join(__dirname, 'certs');
const certificateName = config.name;
const certFilePath = path.join(certPath, `${certificateName}.pem`);
const keyFilePath = path.join(certPath, `${certificateName}.key`);

if (!fs.existsSync(certPath)) {
    fs.mkdirSync(certPath, { recursive: true });
}

if (!fs.existsSync(certFilePath) || !fs.existsSync(keyFilePath)) {
    if (0 !== child_process.spawnSync('dotnet', [
        'dev-certs',
        'https',
        '--export-path',
        certFilePath,
        '--format',
        'Pem',
        '--no-password',
    ], { stdio: 'inherit', }).status) {
        throw new Error("Could not create certificate.");
    }
}

export default defineConfig({
    plugins: [react(), tailwindcss()],
    content: {
        files: [
            "./Apps/**/*.cshtml",
            "./Apps/**/*.tsx",
            "./Libs/**/*.cshtml",
            "./Libs/**/*.tsx",
        ]
    },
    base: '/dist/',
    server: {
        strictPort: true,
        host: "localhost",
        cors: true,
        hmr: {
            protocol: 'wss',
            port: config.vite.server.port,
        },
        https: {
            key: fs.readFileSync(keyFilePath),
            cert: fs.readFileSync(certFilePath),
        }
    },
    define: {
        'process.env': {}
    },
    resolve: {
        alias: {
            '@': path.resolve(__dirname, './Apps/Portal/Assets'),
            '@ui': path.resolve(__dirname, './Libs/UI/Assets')
        }
    },
    build: {
        outDir: path.join(webAppPath, "wwwroot", "dist"),
        emptyOutDir: true,
        manifest: false,
        rolldownOptions: {
            input: entryPoint,
            external: ['jquery'],
            output: {
                globals: {
                    jquery: '$',
                },
                cleanDir: true,
                // format: 'umd',
                entryFileNames: 'js/[name]-dist.js',
                chunkFileNames: 'js/[name]-[hash].js',
                assetFileNames: (asset: any) => {
                    const name = asset.name;
                    if (!name) return 'assets/[name]-dist[extname]';
                    else if (/\.(gif|jpe?g|png|svg)$/.test(name)) return 'assets/images/[name]-dist[extname]';
                    else if (/\.(ttf|woff)$/.test(name)) return 'assets/fonts/[name]-dist[extname]';
                    else if (/\.css$/.test(name)) return 'css/[name]-dist[extname]';
                    return 'assets/[name]-dist[extname]';
                },
                codeSplitting: {
                    minSize: 20000,
                    groups: [
                        {
                            name: 'libs/react',
                            test: /node_modules[\\/]react/,
                            priority: 20,
                        },
                        {
                            name: 'libs/keenthemes',
                            test: /node_modules[\\/]@keenthemes/,
                            priority: 19,
                        },
                        {
                            name: 'libs/vanilla-calendar-pro',
                            test: /node_modules[\\/]vanilla-calendar-pro/,
                            priority: 18,
                        },
                        {
                            name: 'libs/simonwep',
                            test: /node_modules[\\/]@simonwep/,
                            priority: 18,
                        },
                        {
                            name: 'libs/vendor',
                            test: /node_modules/,
                            priority: 10,
                        }
                    ],
                }
            }
        }
    }
} as any);
