export function generateQRCode(elementID, content, width, height) {
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
