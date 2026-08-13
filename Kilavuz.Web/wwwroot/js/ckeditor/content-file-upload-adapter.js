class ContentFileUploadAdapter {
    constructor(loader, uploadUrl, categoryId, pageId) {
        this.loader = loader;
        this.uploadUrl = uploadUrl;
        this.categoryId = categoryId;
        this.pageId = pageId;
    }

    upload() {
        return this.loader.file
            .then(file => new Promise((resolve, reject) => {
                const data = new FormData();
                data.append('file', file);
                
                // Anti-forgery token okuma
                const tokenElement = document.querySelector('input[name="__RequestVerificationToken"]');
                if (tokenElement) {
                    data.append('__RequestVerificationToken', tokenElement.value);
                }

                // Yetki kontrolü için id'ler
                if (this.categoryId) data.append('categoryId', this.categoryId);
                if (this.pageId) data.append('pageId', this.pageId);

                // Use fetch instead of jQuery to avoid jQuery dependency inside ES module
                fetch(this.uploadUrl, {
                    method: 'POST',
                    body: data
                })
                .then(response => {
                    if (!response.ok) {
                        return response.json().then(err => { throw err; });
                    }
                    return response.json();
                })
                .then(response => {
                    resolve({
                        default: response.url,
                        fileName: response.fileName,
                        fileType: response.fileType
                    });
                })
                .catch(error => {
                    let msg = 'Dosya yükleme başarısız';
                    if (error && error.error && error.error.message) {
                        msg = error.error.message;
                    }
                    reject(msg);
                });
            }));
    }

    abort() { }
}
export { ContentFileUploadAdapter };
