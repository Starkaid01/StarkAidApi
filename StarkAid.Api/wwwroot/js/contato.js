document.addEventListener('DOMContentLoaded', () => {
    const contactForm = document.getElementById('contact-form');
    const formMessage = document.getElementById('form-message');

    contactForm.addEventListener('submit', (e) => {
        e.preventDefault();
        
        // Simular envio do formulário (já que é apenas uma amostra)
        const formData = new FormData(contactForm);
        const data = Object.fromEntries(formData);

        // Mostrar mensagem de sucesso
        formMessage.textContent = 'Mensagem enviada com sucesso! Entraremos em contato em breve.';
        formMessage.className = 'form-message success';
        
        // Limpar formulário
        contactForm.reset();
        
        // Esconder mensagem após 5 segundos
        setTimeout(() => {
            formMessage.className = 'form-message';
        }, 5000);
    });
});

