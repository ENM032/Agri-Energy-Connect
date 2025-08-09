// Authentication Page JavaScript Functionality

// Password toggle functionality
function togglePassword(inputId, iconId) {
    const passwordInput = document.getElementById(inputId);
    const toggleIcon = document.getElementById(iconId);
    
    if (passwordInput && toggleIcon) {
        if (passwordInput.type === 'password') {
            passwordInput.type = 'text';
            toggleIcon.classList.remove('fa-eye');
            toggleIcon.classList.add('fa-eye-slash');
        } else {
            passwordInput.type = 'password';
            toggleIcon.classList.remove('fa-eye-slash');
            toggleIcon.classList.add('fa-eye');
        }
    }
}

// Legacy support for single parameter
function togglePassword() {
    if (arguments.length === 0) {
        togglePassword('passwordInput', 'passwordToggleIcon');
    } else {
        togglePassword(arguments[0], arguments[1]);
    }
}

// Password strength checker
function checkPasswordStrength(password) {
    let strength = 0;
    let feedback = [];
    
    // Length check
    if (password.length >= 8) {
        strength += 1;
    } else {
        feedback.push('At least 8 characters');
    }
    
    // Uppercase check
    if (/[A-Z]/.test(password)) {
        strength += 1;
    } else {
        feedback.push('One uppercase letter');
    }
    
    // Lowercase check
    if (/[a-z]/.test(password)) {
        strength += 1;
    } else {
        feedback.push('One lowercase letter');
    }
    
    // Number check
    if (/[0-9]/.test(password)) {
        strength += 1;
    } else {
        feedback.push('One number');
    }
    
    // Special character check
    if (/[^A-Za-z0-9]/.test(password)) {
        strength += 1;
    } else {
        feedback.push('One special character');
    }
    
    return { strength, feedback };
}

// Update password strength indicator
function updatePasswordStrength(password) {
    const strengthBar = document.getElementById('strengthBar');
    const strengthText = document.getElementById('strengthText');
    
    if (!strengthBar || !strengthText) return;
    
    const { strength, feedback } = checkPasswordStrength(password);
    
    // Update strength bar
    const percentage = (strength / 5) * 100;
    strengthBar.style.width = percentage + '%';
    
    // Remove existing strength classes
    strengthBar.classList.remove('strength-weak', 'strength-fair', 'strength-good', 'strength-strong');
    
    // Update strength indicator
    let strengthClass = '';
    let strengthLabel = '';
    
    if (strength === 0 || password.length === 0) {
        strengthLabel = 'Password strength';
        strengthBar.style.width = '0%';
    } else if (strength <= 2) {
        strengthClass = 'strength-weak';
        strengthLabel = 'Weak';
    } else if (strength === 3) {
        strengthClass = 'strength-fair';
        strengthLabel = 'Fair';
    } else if (strength === 4) {
        strengthClass = 'strength-good';
        strengthLabel = 'Good';
    } else {
        strengthClass = 'strength-strong';
        strengthLabel = 'Strong';
    }
    
    if (strengthClass) {
        strengthBar.classList.add(strengthClass);
    }
    
    strengthText.textContent = strengthLabel;
    
    // Show feedback for weak passwords
    if (strength < 4 && password.length > 0) {
        strengthText.textContent += ' - Need: ' + feedback.join(', ');
    }
}

// Form validation enhancement
function enhanceFormValidation() {
    const forms = document.querySelectorAll('form');
    
    forms.forEach(form => {
        const inputs = form.querySelectorAll('input[required], select[required]');
        
        inputs.forEach(input => {
            input.addEventListener('blur', function() {
                validateField(this);
            });
            
            input.addEventListener('input', function() {
                if (this.classList.contains('is-invalid')) {
                    validateField(this);
                }
            });
        });
        
        form.addEventListener('submit', function(e) {
            let isValid = true;
            
            inputs.forEach(input => {
                if (!validateField(input)) {
                    isValid = false;
                }
            });
            
            // Check terms checkbox if it exists
            const termsCheckbox = form.querySelector('#termsCheckbox');
            if (termsCheckbox && !termsCheckbox.checked) {
                isValid = false;
                showFieldError(termsCheckbox, 'You must agree to the terms and conditions');
            }
            
            if (!isValid) {
                e.preventDefault();
            }
        });
    });
}

// Validate individual field
function validateField(field) {
    const value = field.value.trim();
    let isValid = true;
    let errorMessage = '';
    
    // Clear previous validation state
    field.classList.remove('is-valid', 'is-invalid');
    clearFieldError(field);
    
    // Required field check
    if (field.hasAttribute('required') && !value) {
        isValid = false;
        errorMessage = 'This field is required';
    }
    
    // Email validation
    if (field.type === 'email' && value) {
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        if (!emailRegex.test(value)) {
            isValid = false;
            errorMessage = 'Please enter a valid email address';
        }
    }
    
    // Password validation
    if (field.type === 'password' && value) {
        const { strength } = checkPasswordStrength(value);
        if (strength < 3) {
            isValid = false;
            errorMessage = 'Password is too weak';
        }
    }
    
    // Confirm password validation
    if (field.id === 'confirmPasswordInput' && value) {
        const passwordField = document.getElementById('passwordInput');
        if (passwordField && value !== passwordField.value) {
            isValid = false;
            errorMessage = 'Passwords do not match';
        }
    }
    
    // Update field appearance
    if (isValid) {
        field.classList.add('is-valid');
    } else {
        field.classList.add('is-invalid');
        showFieldError(field, errorMessage);
    }
    
    return isValid;
}

// Show field error
function showFieldError(field, message) {
    clearFieldError(field);
    
    const errorElement = document.createElement('div');
    errorElement.className = 'field-error text-danger';
    errorElement.textContent = message;
    errorElement.style.fontSize = '0.875rem';
    errorElement.style.marginTop = '5px';
    
    field.parentNode.appendChild(errorElement);
}

// Clear field error
function clearFieldError(field) {
    const existingError = field.parentNode.querySelector('.field-error');
    if (existingError) {
        existingError.remove();
    }
}

// Initialize authentication page functionality
document.addEventListener('DOMContentLoaded', function() {
    // Password strength monitoring
    const passwordInput = document.getElementById('passwordInput');
    if (passwordInput) {
        passwordInput.addEventListener('input', function() {
            updatePasswordStrength(this.value);
        });
    }
    
    // Confirm password matching
    const confirmPasswordInput = document.getElementById('confirmPasswordInput');
    if (confirmPasswordInput && passwordInput) {
        confirmPasswordInput.addEventListener('input', function() {
            validateField(this);
        });
        
        passwordInput.addEventListener('input', function() {
            if (confirmPasswordInput.value) {
                validateField(confirmPasswordInput);
            }
        });
    }
    
    // Enhanced form validation
    enhanceFormValidation();
    
    // Add loading state to submit buttons
    const submitButtons = document.querySelectorAll('button[type="submit"]');
    submitButtons.forEach(button => {
        button.addEventListener('click', function() {
            const form = this.closest('form');
            if (form && form.checkValidity()) {
                this.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Processing...';
                this.disabled = true;
            }
        });
    });
    
    // Add smooth animations to form elements
    const formGroups = document.querySelectorAll('.form-group');
    formGroups.forEach((group, index) => {
        group.style.animationDelay = (index * 0.1) + 's';
        group.classList.add('fade-in');
    });
});

// Add CSS for validation states
const validationStyles = `
    .auth-input.is-valid {
        border-color: #28a745;
        background-image: url("data:image/svg+xml,%3csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 8 8'%3e%3cpath fill='%2328a745' d='m2.3 6.73.94-.94 1.44 1.44L7.4 4.5l.94.94L4.66 9.2z'/%3e%3c/svg%3e");
        background-repeat: no-repeat;
        background-position: right calc(0.375em + 0.1875rem) center;
        background-size: calc(0.75em + 0.375rem) calc(0.75em + 0.375rem);
    }
    
    .auth-input.is-invalid {
        border-color: #dc3545;
        background-image: url("data:image/svg+xml,%3csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 12 12' width='12' height='12' fill='none' stroke='%23dc3545'%3e%3ccircle cx='6' cy='6' r='4.5'/%3e%3cpath d='m5.8 4.6 2.4 2.4m0-2.4L5.8 7'/%3e%3c/svg%3e");
        background-repeat: no-repeat;
        background-position: right calc(0.375em + 0.1875rem) center;
        background-size: calc(0.75em + 0.375rem) calc(0.75em + 0.375rem);
    }
`;

// Inject validation styles
const styleSheet = document.createElement('style');
styleSheet.textContent = validationStyles;
document.head.appendChild(styleSheet);