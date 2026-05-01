import type nJQuery from 'jquery';

declare global {
    // jQuery Global
    var $: typeof nJQuery;
    var jQuery: typeof nJQuery;

    // Metronic Global Components
    // Kita gunakan 'any' untuk instance jika tidak ingin mengimpor tipe detailnya, 
    // tapi kita definisikan static methods-nya.
    interface KTMenu {
        createInstances(): void;
        getInstance(element: HTMLElement): KTMenu | null;
        hide(element: HTMLElement): void;
        show(element: HTMLElement): void;
    }

    interface KTUtil {
        init(): void;
        getById(id: string): HTMLElement | null;
        on(element: HTMLElement, name: string, handler: Function): void;
    }

    interface Window {
        $: typeof nJQuery;
        jQuery: typeof nJQuery;

        // Daftarkan komponen ke Window agar tidak error saat dipanggil di .ts/.tsx
        KTMenu: KTMenu;
        KTUtil: KTUtil;
        KTSwapper: any;
        KTScroll: any;
        KTToggle: any;
        KTDrawer: any;
    }

    // Memungkinkan pemanggilan langsung tanpa window. prefix
    var KTMenu: KTMenu;
    var KTUtil: KTUtil;
}

export { };