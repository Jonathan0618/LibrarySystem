document.querySelectorAll('.reference-edit').forEach(button => {
    button.addEventListener('click', () => {
        document.querySelector('[name="Input.Id"]').value = button.dataset.id;
        document.querySelector('[name="Input.Type"]').value = button.dataset.type;
        document.querySelector('[name="Input.Name"]').value = button.dataset.name;
        document.querySelector('[name="Input.Description"]').value = button.dataset.description || '';
        document.querySelector('[name="Input.Name"]').focus();
        window.scrollTo({ top: 0, behavior: 'smooth' });
    });
});
