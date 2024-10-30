document.addEventListener('DOMContentLoaded', function() {
    const newEmailForm = document.querySelector('form[action="/ContactInformation/InitiateEmailChange"]');
    if (newEmailForm) {
        newEmailForm.addEventListener('submit', function(e) {
            const newEmail = document.getElementById('newEmail').value;
            const confirmNewEmail = document.getElementById('confirmNewEmail').value;

            if (newEmail !== confirmNewEmail) {
                e.preventDefault();
                alert('Die E-Mail-Adressen stimmen nicht überein.');
                return false;
            }
        });
    }
});
