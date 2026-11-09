import assert from 'node:assert/strict';
import { afterEach, mock, test } from 'node:test';
import { ShowSuccess } from '../Mobile.MAUI/wwwroot/js/toastifyPresets.js';

const originalWindow = globalThis.window;

afterEach(() => {
    if (originalWindow === undefined) {
        delete globalThis.window;
    } else {
        globalThis.window = originalWindow;
    }
    mock.restoreAll();
});

test('ordinary success notifications keep the three-second default', () => {
    let options;
    let shown = 0;
    globalThis.window = {
        Toastify: value => {
            options = value;
            return { showToast: () => shown++ };
        },
    };

    assert.equal(ShowSuccess('Scanned fulfillment'), true);
    assert.equal(options.duration, 3000);
    assert.equal(shown, 1);
});

test('save confirmation passes the eight-second duration to Toastify once', () => {
    let options;
    let shown = 0;
    globalThis.window = {
        Toastify: value => {
            options = value;
            return { showToast: () => shown++ };
        },
    };

    assert.equal(ShowSuccess('Trip ticket saved successfully.', 8000), true);
    assert.equal(options.duration, 8000);
    assert.equal(options.text, 'Trip ticket saved successfully.');
    assert.equal(options.className, 'toast-success');
    assert.equal(shown, 1);
});

test('missing Toastify reports failure so the caller can show a fallback', () => {
    globalThis.window = {};
    const errors = mock.method(console, 'error', () => {});

    assert.equal(ShowSuccess('Trip ticket saved successfully.', 8000), false);
    assert.equal(errors.mock.callCount(), 1);
});

test('rendering exceptions report failure instead of pretending confirmation appeared', () => {
    globalThis.window = {
        Toastify: () => ({
            showToast: () => { throw new Error('Rendering failed'); },
        }),
    };
    const errors = mock.method(console, 'error', () => {});

    assert.equal(ShowSuccess('Trip ticket saved successfully.', 8000), false);
    assert.equal(errors.mock.callCount(), 1);
});
