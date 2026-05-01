// Mengimpor tipe data dari node_modules agar IntelliSense tahu method jQuery (seperti .ajax, .on, dll)
import type nJQuery from 'jquery';

declare global {
    // Mendefinisikan variabel global yang tersedia di browser (dari folder wwwroot/lib)
    var $: typeof nJQuery;
    var jQuery: typeof nJQuery;

    // Menambahkan properti khusus ke objek 'window'
    interface Window {
        $: typeof nJQuery;
        jQuery: typeof nJQuery;

        // Objek Global Metronic (Keenthemes)
        KTUtil?: any;
        KTMenu?: any;
        KTSwapper?: any;
        KTScroll?: any;
        KTToggle?: any;

        // Objek Global Vuexy jika digunakan
        helpers?: any;
    }
}

// Penting: File ini harus dianggap sebagai module oleh TS
export { };