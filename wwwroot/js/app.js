document.getElementById('claimForm')?.addEventListener('submit', function (e) {
  const sessions = parseInt(document.querySelector('[name="number_of_sessions"]').value) || 0;
  const hours = parseInt(document.querySelector('[name="number_of_hours"]').value) || 0;
  const rate = parseInt(document.querySelector('[name="amount_of_rate"]').value) || 0;
  const module = document.querySelector('[name="module_name"]').value.trim();
  const faculty = document.querySelector('[name="faculty_name"]').value.trim();

  if (sessions <= 0 || hours <= 0 || rate <= 0) {
    e.preventDefault();
    alert('Sessions, hours, and rate must be positive numbers.');
    return false;
  }
  if (!module || !faculty) {
    e.preventDefault();
    alert('Module and faculty names are required.');
    return false;
  }

  // Confirm submit
  const confirmSubmit = document.getElementById('confirmSubmitModal');
  if (confirmSubmit) {
    e.preventDefault();
    const modal = new bootstrap.Modal(confirmSubmit);
    document.getElementById('confirm-total').textContent =
      document.getElementById('totalDisplay').value || 'R 0.00';
    modal.show();
    document.getElementById('confirmSubmitBtn').onclick = () => {
      modal.hide();
      e.target.submit();
    };
  }

  // Auto-dismiss alerts after 5s
  const alerts = document.querySelectorAll('.alert');
  alerts.forEach(alert => {
    setTimeout(() => {
      const bsAlert = new bootstrap.Alert(alert);
      bsAlert.close();
    }, 5000);
  });
});

// Global form validation with shake
document.querySelectorAll('form').forEach(form => {
  form.addEventListener('submit', (e) => {
    if (!form.checkValidity()) {
      e.preventDefault();
      e.stopPropagation();
      form.classList.add('was-validated', 'animate__animated', 'animate__shakeX');
    } else {
      form.classList.remove('animate__shakeX');
    }
  });
});

// Dark/Light Switcher
function toggleTheme() {
  document.documentElement.dataset.theme =
    document.documentElement.dataset.theme === 'light' ? 'dark' : 'light';
  localStorage.setItem('theme', document.documentElement.dataset.theme);
}

// On load
document.documentElement.dataset.theme = localStorage.getItem('theme') || 'dark';

// HR export helpers
window.downloadCsv = (url) => {
  window.location.href = url;
};
window.downloadPdf = (url) => {
  window.location.href = url;
};
