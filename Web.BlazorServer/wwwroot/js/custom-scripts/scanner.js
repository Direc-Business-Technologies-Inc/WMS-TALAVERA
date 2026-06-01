window.dotnetInstance;

export function setdotnetInstance(dotnetInstance) {
    window.dotnetInstance = dotnetInstance;
}

const SCANNER_STATES = {
    IDLE: 0,
    SCAN: 1,
    DISPLAY: 2,
    REVIEW: 3,
    PROMPT: 4
};

class Scanner {
    constructor() {
        this.state = {
            currentState: SCANNER_STATES.IDLE,
            scanBuffer: '',
            scanTimeout: null
        };

        this._html5QrCode = null;
        this._processed = false;

        this.handleKeyPress = this.handleKeyPress.bind(this);
    }

    handleKeyPress(event) {
        if (this.state.currentState !== SCANNER_STATES.SCAN) return;

        if (this.state.scanTimeout) {
            clearTimeout(this.state.scanTimeout);
        }

        this.state.scanBuffer += event.key;
        console.log("[SCAN] SCANNER: scanned ", this.state.scanBuffer);

        this.state.scanTimeout = setTimeout(() => {
            if (this.state.scanBuffer) {
                this.processScan(this.state.scanBuffer);
                this.state.scanBuffer = '';
            }
        }, 100);
    }

    processScan(scannedData) {
        console.log("[SCAN] DISPLAY: ", scannedData);
        dotnetInstance.invokeMethodAsync('UpdateScannedData', scannedData);

        this.state.currentState = SCANNER_STATES.DISPLAY;
        dotnetInstance.invokeMethodAsync('UpdateState', SCANNER_STATES.DISPLAY);
    }

    async startScan() {
        this._processed = false;
        this._html5QrCode = new Html5Qrcode("reader");

        try {
            await this._html5QrCode.start(
                { facingMode: "environment" },
                { fps: 10, qrbox: { width: 220, height: 220 } },
                (decodedText) => {
                    if (!this._processed) {
                        this._processed = true;
                        this.processScan(decodedText);
                    }
                },
                (_errorMessage) => { /* ignore per-frame scan misses */ }
            );
            this.state.currentState = SCANNER_STATES.SCAN;
            dotnetInstance.invokeMethodAsync('UpdateState', SCANNER_STATES.SCAN);
        } catch (err) {
            console.error("[SCAN] Camera error:", err);
            dotnetInstance.invokeMethodAsync('UpdateState', SCANNER_STATES.IDLE);
        }

        // Also keep USB/keyboard scanner support
        this._boundKeyPress = (e) => this.handleKeyPress(e);
        document.addEventListener('keypress', this._boundKeyPress);
    }

    restartScan() {
        this._processed = false;
        this.state.scanBuffer = '';
        this.state.currentState = SCANNER_STATES.SCAN;
        dotnetInstance.invokeMethodAsync('UpdateState', SCANNER_STATES.SCAN);
    }

    promptNextScan() {
        this.state.currentState = SCANNER_STATES.PROMPT;
        dotnetInstance.invokeMethodAsync('UpdateState', SCANNER_STATES.PROMPT);
    }

    resetToIdle() {
        this.state.currentState = SCANNER_STATES.IDLE;
        dotnetInstance.invokeMethodAsync('UpdateState', SCANNER_STATES.IDLE);
    }

    async destroy() {
        if (this._boundKeyPress) {
            document.removeEventListener('keypress', this._boundKeyPress);
            this._boundKeyPress = null;
        }
        if (this._html5QrCode) {
            try {
                if (this._html5QrCode.isScanning) {
                    await this._html5QrCode.stop();
                }
                this._html5QrCode.clear();
            } catch (e) {
                console.warn("[DESTROY] Error stopping camera:", e);
            }
            this._html5QrCode = null;
        }
    }
}

let scanner = null;

export function initializeScanner() {
    scanner = new Scanner();
}

export async function startScan() {
    if (scanner) await scanner.startScan();
}

export function promptNextScan() {
    if (scanner) scanner.promptNextScan();
}

export function restartScan() {
    if (scanner) scanner.restartScan();
}

export function resetToIdle() {
    if (scanner) scanner.resetToIdle();
}

export async function destroyScanner() {
    if (scanner) {
        await scanner.destroy();
        scanner = null;
    }
}
