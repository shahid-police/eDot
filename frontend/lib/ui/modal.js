export async function closeModal(id) {
  const element = document.getElementById(id);
  if (!element) {
    return;
  }
  const { Modal } = await import("bootstrap/dist/js/bootstrap.esm");
  Modal.getOrCreateInstance(element).hide();
}
