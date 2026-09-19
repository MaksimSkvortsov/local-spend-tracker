let returnFocusElement;
let returnFocusSelector;

export function activateCardsDialog(dialog) {
    if (!dialog) return;

    const previous = document.activeElement;
    if (previous && !previous.closest('.cards-modal[role="dialog"]')) {
        returnFocusElement = previous;
    }
    const combineCardId = dialog.querySelector('#source-card')?.value;
    const deleteCardId = dialog.dataset.deleteCardId;
    if (combineCardId) returnFocusSelector = `[data-combine-card-id="${combineCardId}"]`;
    if (deleteCardId) returnFocusSelector = `button[data-delete-card-id="${deleteCardId}"]`;

    const focusable = () => [...dialog.querySelectorAll('button:not(:disabled), select:not(:disabled), input:not(:disabled)')];
    const items = focusable();
    items[0]?.focus();

    const onKeyDown = event => {
        if (event.key !== 'Tab') return;

        const controls = focusable();
        if (!controls.length) {
            event.preventDefault();
            return;
        }

        const first = controls[0];
        const last = controls[controls.length - 1];
        if (event.shiftKey && document.activeElement === first) {
            event.preventDefault();
            last.focus();
        } else if (!event.shiftKey && document.activeElement === last) {
            event.preventDefault();
            first.focus();
        }
    };

    dialog.addEventListener('keydown', onKeyDown);
    const observer = new MutationObserver(() => {
        if (dialog.isConnected) return;

        observer.disconnect();
        dialog.removeEventListener('keydown', onKeyDown);
        if (!document.querySelector('.cards-modal[role="dialog"]')) {
            const fallback = (returnFocusSelector ? document.querySelector(returnFocusSelector) : null)
                ?? document.querySelector('.cards-search');
            (returnFocusElement?.isConnected && returnFocusElement !== document.body
                ? returnFocusElement
                : fallback)?.focus();
            returnFocusElement = undefined;
            returnFocusSelector = undefined;
        }
    });
    observer.observe(document.body, { childList: true, subtree: true });
}
