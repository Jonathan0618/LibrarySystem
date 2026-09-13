document.getElementById('generate-barcode')?.addEventListener('click', () => {
    const random = new Uint32Array(2);
    crypto.getRandomValues(random);
    const suffix = Array.from(random, value => value.toString(36).padStart(7, '0')).join('').toUpperCase();
    document.getElementById('Copy_Barcode').value = `LIB-${new Date().getFullYear()}-${suffix}`;
});
