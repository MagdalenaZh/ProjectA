let deleteProjectFormToSubmit = null;

document.addEventListener("DOMContentLoaded", function () {
    const deleteModalElement = document.getElementById("deleteProjectModal");
    const deleteTitleElement = document.getElementById("deleteProjectTitle");
    const confirmDeleteButton = document.getElementById("confirmDeleteProjectButton");

    if (!deleteModalElement || !deleteTitleElement || !confirmDeleteButton) {
        return;
    }

    deleteModalElement.addEventListener("show.bs.modal", function (event) {
        const triggerButton = event.relatedTarget;
        if (!triggerButton) return;

        const projectTitle = triggerButton.getAttribute("data-project-title") || "";
        const formId = triggerButton.getAttribute("data-form-id") || "";

        deleteTitleElement.textContent = projectTitle;
        deleteProjectFormToSubmit = document.getElementById(formId);
    });

    confirmDeleteButton.addEventListener("click", function () {
        if (deleteProjectFormToSubmit) {
            deleteProjectFormToSubmit.submit();
        }
    });
});