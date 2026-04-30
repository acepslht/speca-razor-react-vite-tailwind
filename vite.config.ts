import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';
import { glob } from 'glob';
import path from 'path';
import child_process from 'child_process';
import fs from 'fs';

const getAssetsEntries = () => {
    const entries: Record<string, string> = {};
    const files = glob.sync('{Apps,Libs}/**/Assets/Entries/**/*.{jsx,tsx,vue,js,ts}');
    files.forEach(file => {
        const parts = file.split(path.sep);
        const category = parts[0].toLowerCase();
        const projectFolder = parts[1].toLowerCase().replace(/\./g, '-');
        const fileName = path.parse(file).name.toLowerCase();
        entries[`${category}/${projectFolder}/${fileName}`] = path.resolve(__dirname, file);
    });
    return entries;
};

const entryPoint = {
    ...getAssetsEntries(),
    'libs/ui/app': path.resolve(__dirname, 'Libs', 'UI', 'Assets', 'app.css'),
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

const isProduction = process.env.NODE_ENV === 'production';

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
    base: isProduction ? '/dist/' : '/',
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
    build: {
        outDir: path.join(webAppPath, "wwwroot", "dist"),
        emptyOutDir: true,
        manifest: true,
        rolldownOptions: {
            input: entryPoint,
            output: {
                cleanDir: true,
                // format: 'umd',
                entryFileNames: '[name]-dist.js',
                //assetFileNames: 'assets/[name]-[hash][extname]',
                chunkFileNames: '[name]-[hash].js',
                assetFileNames: ({ name }: { name: string }) => {
                    if (!name) return '[name]-[hash][extname]';
                    else if (/\.(gif|jpe?g|png|svg)$/.test(name)) return 'img/[name]-[hash][extname]';
                    else if (/\.css$/.test(name)) return 'css/[name]-[hash][extname]';
                    return '[name]-[hash][extname]';
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
