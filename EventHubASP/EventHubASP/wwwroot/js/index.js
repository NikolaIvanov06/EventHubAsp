// Smooth scrolling for anchor links
document.querySelectorAll('a[href^="#"]').forEach(anchor => {
    anchor.addEventListener('click', function (e) {
        e.preventDefault();
        const targetId = this.getAttribute('href');
        const targetElement = document.querySelector(targetId);
        if (targetElement) {
            targetElement.scrollIntoView({
                behavior: 'smooth',
                block: 'start'
            });
        }
    });
});

// Add scroll animations with parallax effect
window.addEventListener('scroll', () => {
    const elements = document.querySelectorAll('.features, .cta, .hero, .feature-card');
    elements.forEach(element => {
        const rect = element.getBoundingClientRect();
        const isVisible = rect.top < window.innerHeight - 100 && rect.bottom >= 0;

        if (isVisible) {
            // Fade-in effect
            element.style.opacity = 1;
            element.style.transform = 'translateY(0)';

            // Parallax effect for the hero section
            if (element.classList.contains('hero')) {
                const scrollY = window.scrollY;
                element.style.backgroundPositionY = `${scrollY * 0.5}px`;
            }

            // Scale-up effect for feature cards
            if (element.classList.contains('feature-card')) {
                element.style.transform = 'scale(1.05)';
                element.style.boxShadow = '0 10px 20px rgba(0, 0, 0, 0.3)';
            }
        } else {
            // Reset animations for elements out of view
            if (!element.classList.contains('hero')) {
                element.style.opacity = 0;
                element.style.transform = 'translateY(20px)';
            }

            // Reset feature cards
            if (element.classList.contains('feature-card')) {
                element.style.transform = 'scale(1)';
                element.style.boxShadow = '0 4px 6px rgba(0, 0, 0, 0.1)';
            }
        }
    });
});

// Add hover effects for buttons
document.querySelectorAll('.btn').forEach(button => {
    button.addEventListener('mouseenter', () => {
        button.style.transform = 'scale(1.1)';
        button.style.backgroundColor = '#ff6f61'; // Bright accent color
    });

    button.addEventListener('mouseleave', () => {
        button.style.transform = 'scale(1)';
        button.style.backgroundColor = '#222';
    });
});

// Add a subtle background animation for the hero section
const hero = document.querySelector('.hero');
if (hero) {
    hero.style.transition = 'background-position 0.5s ease-out';
}

// Add a ripple effect for buttons on click
document.querySelectorAll('.btn').forEach(button => {
    button.addEventListener('click', (e) => {
        const ripple = document.createElement('span');
        ripple.classList.add('ripple');
        ripple.style.left = `${e.offsetX}px`;
        ripple.style.top = `${e.offsetY}px`;
        button.appendChild(ripple);

        setTimeout(() => ripple.remove(), 500);
    });
});

// Add a dynamic year in the footer (if you have one)
const footerYear = document.querySelector('#footer-year');
if (footerYear) {
    footerYear.textContent = new Date().getFullYear();
}