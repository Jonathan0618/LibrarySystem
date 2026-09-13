const toggle = document.querySelector('.password-toggle');
const password = document.getElementById('Input_Password');

toggle?.addEventListener('click', () => {
    const isVisible = password.type === 'text';
    password.type = isVisible ? 'password' : 'text';
    toggle.textContent = isVisible ? 'Show' : 'Hide';
    toggle.setAttribute('aria-label', isVisible ? 'Show password' : 'Hide password');
    toggle.setAttribute('aria-pressed', String(!isVisible));
});
