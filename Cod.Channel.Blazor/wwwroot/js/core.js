window.getUserAgent = () => {
    return navigator.userAgent;
};

window.getLocate = () => {
    return navigator.language;
};

window.blazorDocumentReady = function () {
};

window.generateQRCode = (elementID, content, width, height) => {
    if (!elementID) {
        return;
    }

    if (!content) {
        return;
    }

    if (!width) {
        width = 128;
    }

    if (!height) {
        height = 128;
    }

    $('#' + elementID).qrcode({
        width: width,
        height: height,
        text: content
    });
};

window.getCache = key => {
    if (key) {
        return localStorage.getItem(key);
    } else {
        return null;
    }
};

window.setCache = (key, value) => {
    if (key) {
        localStorage.setItem(key, value);
    }
};

window.copyToClipboard = str => {
    const el = document.createElement('textarea');
    el.value = str;
    el.setAttribute('readonly', '');
    el.style.position = 'absolute';
    el.style.left = '-9999px';
    document.body.appendChild(el);
    el.select();
    document.execCommand('copy');
    document.body.removeChild(el);
};

Blazor.start({
    loadBootResource: function (type, name, defaultUri, integrity) {
        if (type !== 'dotnetjs' && location.hostname !== 'localhost') {
            return (async function () {
                const response = await fetch(defaultUri + '.br', { cache: 'no-cache' });
                if (!response.ok) {
                    throw new Error(response.statusText);
                }
                const originalResponseBuffer = await response.arrayBuffer();
                const originalResponseArray = new Int8Array(originalResponseBuffer);
                const decompressedResponseArray = BrotliDecode(originalResponseArray);
                const contentType = type ===
                    'dotnetwasm' ? 'application/wasm' : 'application/octet-stream';
                return new Response(decompressedResponseArray,
                    { headers: { 'content-type': contentType } });
            })();
        }
    }
});