export function getCache(key) {
    if (key) {
        return localStorage.getItem(key);
    } else {
        return null;
    }
};

export function setCache(key, value) {
    if (key) {
        localStorage.setItem(key, value);
    } else {
        localStorage.removeItem(key);
    }
};
