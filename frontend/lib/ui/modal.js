async function bootstrapModal(id) {
  const element = document.getElementById(id);
  if (!element) {
    return null;
  }
  const { Modal: BootstrapModal } = await import("bootstrap/dist/js/bootstrap.esm");
  return BootstrapModal.getOrCreateInstance(element);
}

export async function closeModal(id) {
  const modal = await bootstrapModal(id);
  modal?.hide();
}

export async function openModal(id) {
  const modal = await bootstrapModal(id);
  modal?.show();
}
