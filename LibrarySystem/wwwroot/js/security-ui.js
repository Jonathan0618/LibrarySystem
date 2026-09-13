document.querySelectorAll('[data-confirm]').forEach((element) => {
    const eventName = element.tagName === 'FORM' ? 'submit' : 'click';
    element.addEventListener(eventName, (event) => {
        if (!window.confirm(element.dataset.confirm)) {
            event.preventDefault();
        }
    });
});
