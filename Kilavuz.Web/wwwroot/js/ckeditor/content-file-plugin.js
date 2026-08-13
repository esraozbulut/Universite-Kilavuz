import { Plugin, Command, Widget, toWidget, ButtonView } from 'ckeditor5';
import { ContentFileUploadAdapter } from './content-file-upload-adapter.js';

class InsertContentFileCommand extends Command {
    execute(options) {
        const editor = this.editor;
        editor.model.change(writer => {
            const contentFile = writer.createElement('contentFile', {
                fileUrl: options.fileUrl,
                fileName: options.fileName,
                fileType: options.fileType
            });
            editor.model.insertContent(contentFile);
        });
    }

    refresh() {
        const model = this.editor.model;
        const selection = model.document.selection;
        const allowedIn = model.schema.findAllowedParent(selection.getFirstPosition(), 'contentFile');
        this.isEnabled = allowedIn !== null;
    }
}

class ContentFileEditing extends Plugin {
    static get requires() {
        return [Widget];
    }

    init() {
        const editor = this.editor;

        this._defineSchema();
        this._defineConverters();

        editor.commands.add('insertContentFile', new InsertContentFileCommand(editor));
    }

    _defineSchema() {
        const schema = this.editor.model.schema;

        schema.register('contentFile', {
            isObject: true,
            isBlock: true,
            allowWhere: '$block',
            allowAttributes: ['fileUrl', 'fileName', 'fileType']
        });
    }

    _defineConverters() {
        const conversion = this.editor.conversion;

        // Upcast: HTML -> Model
        // <a href="..." class="content-file" data-file-name="..." data-file-type="...">Ornek.pdf</a>
        conversion.for('upcast').elementToElement({
            view: {
                name: 'a',
                classes: 'content-file'
            },
            model: (viewElement, { writer }) => {
                return writer.createElement('contentFile', {
                    fileUrl: viewElement.getAttribute('href'),
                    fileName: viewElement.getAttribute('data-file-name'),
                    fileType: viewElement.getAttribute('data-file-type')
                });
            }
        });

        // Data Downcast: Model -> Clean HTML for Database
        conversion.for('dataDowncast').elementToElement({
            model: 'contentFile',
            view: (modelElement, { writer }) => {
                const url = modelElement.getAttribute('fileUrl');
                const name = modelElement.getAttribute('fileName') || url;
                const type = modelElement.getAttribute('fileType') || '';

                let icon = '📄';
                let displayType = 'Dosya';
                if (type.includes('pdf')) { icon = '📕'; displayType = 'PDF Dosyası'; }
                else if (type.includes('word') || name.endsWith('docx')) { icon = '📘'; displayType = 'Word Dosyası'; }
                else if (type.includes('excel') || type.includes('spreadsheet') || name.endsWith('xlsx') || name.endsWith('csv')) { icon = '📗'; displayType = 'Excel Dosyası'; }
                else if (type.includes('presentation') || name.endsWith('pptx')) { icon = '📙'; displayType = 'PowerPoint Dosyası'; }

                return writer.createRawElement('a', {
                    href: url,
                    class: 'content-file',
                    'data-file-name': name,
                    'data-file-type': type,
                    target: '_blank',
                    rel: 'noopener noreferrer'
                }, function(domElement) {
                    domElement.innerHTML = `
                        <span class="content-file-icon">${icon}</span>
                        <span class="content-file-info">
                            <span class="content-file-name">${name.replace(/</g, "&lt;").replace(/>/g, "&gt;")}</span>
                            <span class="content-file-type">${displayType}</span>
                        </span>
                        <span class="content-file-actions">
                            <span class="content-file-action">Aç</span>
                            <span class="content-file-action">İndir</span>
                        </span>
                    `;
                });
            }
        });

        // Editing Downcast: Model -> Widget UI in Editor
        conversion.for('editingDowncast').elementToElement({
            model: 'contentFile',
            view: (modelElement, { writer }) => {
                const url = modelElement.getAttribute('fileUrl');
                const name = modelElement.getAttribute('fileName') || url;
                const type = modelElement.getAttribute('fileType') || '';

                const wrapper = writer.createContainerElement('div', { class: 'content-file-widget-wrapper' });

                const card = writer.createUIElement('a', { 
                    class: 'content-file',
                    href: url,
                    target: '_blank',
                    rel: 'noopener noreferrer'
                }, function(domDocument) {
                    const domElement = domDocument.createElement('a');
                    // We duplicate the attributes so the raw element has them
                    domElement.setAttribute('class', 'content-file');
                    domElement.setAttribute('href', url);
                    domElement.setAttribute('target', '_blank');
                    domElement.setAttribute('rel', 'noopener noreferrer');
                    
                    let icon = '📄';
                    let displayType = 'Dosya';
                    if (type.includes('pdf')) { icon = '📕'; displayType = 'PDF Dosyası'; }
                    else if (type.includes('word') || name.endsWith('docx')) { icon = '📘'; displayType = 'Word Dosyası'; }
                    else if (type.includes('excel') || type.includes('spreadsheet') || name.endsWith('xlsx') || name.endsWith('csv')) { icon = '📗'; displayType = 'Excel Dosyası'; }
                    else if (type.includes('presentation') || name.endsWith('pptx')) { icon = '📙'; displayType = 'PowerPoint Dosyası'; }

                    domElement.innerHTML = `
                        <span class="content-file-icon">${icon}</span>
                        <span class="content-file-info">
                            <span class="content-file-name">${name.replace(/</g, "&lt;").replace(/>/g, "&gt;")}</span>
                            <span class="content-file-type">${displayType}</span>
                        </span>
                        <span class="content-file-actions">
                            <span class="content-file-action">Aç</span>
                            <span class="content-file-action">İndir</span>
                        </span>
                    `;
                    return domElement;
                });

                writer.insert(writer.createPositionAt(wrapper, 0), card);
                return toWidget(wrapper, writer, { label: 'Dosya Eklentisi' });
            }
        });
    }
}

class ContentFileUI extends Plugin {
    init() {
        const editor = this.editor;

        editor.ui.componentFactory.add('insertContentFile', locale => {
            const button = document.createElement('button');
            button.classList.add('ck', 'ck-button', 'ck-off');
            button.setAttribute('type', 'button');
            button.setAttribute('tabindex', '-1');
            
            button.innerHTML = `
                <svg viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg" width="20" height="20">
                    <path d="M16.5 6v11.5c0 2.21-1.79 4-4 4s-4-1.79-4-4V5a2.5 2.5 0 0 1 5 0v10.5c0 .55-.45 1-1 1s-1-.45-1-1V6H10v9.5a2.5 2.5 0 0 0 5 0V5c0-1.38-1.12-2.5-2.5-2.5S10 3.62 10 5v12.5a4 4 0 0 0 8 0V6h-1.5z"/>
                </svg>
                <span class="ck-button__label" style="display:none;">Dosya Ekle</span>
            `;

            // Tooltip handler
            button.addEventListener('mouseenter', () => { button.classList.add('ck-tooltip-text'); button.setAttribute('data-cke-tooltip-text', 'Dosya Ekle'); });
            
            const fileInput = document.createElement('input');
            fileInput.type = 'file';
            fileInput.accept = '.pdf,.docx,.xlsx,.pptx,.csv';
            fileInput.style.display = 'none';
            document.body.appendChild(fileInput);

            button.addEventListener('click', () => {
                const command = editor.commands.get('insertContentFile');
                if (command.isEnabled) {
                    fileInput.click();
                }
            });

            fileInput.addEventListener('change', (e) => {
                const file = e.target.files[0];
                if (file) {
                    const loader = { file: Promise.resolve(file) };
                    const adapter = new ContentFileUploadAdapter(loader, window.contentFileUploadUrl, window.pageCategoryId, window.pageId);
                    
                    editor.model.change(writer => {
                        // Show a loading notification or simply upload
                        adapter.upload().then(response => {
                            editor.execute('insertContentFile', {
                                fileUrl: response.default,
                                fileName: response.fileName,
                                fileType: response.fileType
                            });
                        }).catch(err => {
                            alert(err);
                        }).finally(() => {
                            fileInput.value = ''; // reset
                        });
                    });
                }
            });

            // We must import ButtonView dynamically or return a proper view.
            return this._createButtonView(locale, fileInput);
        });
    }

    _createButtonView(locale, fileInput) {
        const view = new ButtonView(locale);

        view.set({
            label: 'Sayfa İçi Dosya Ekle',
            icon: '<svg viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path d="M16.5 6v11.5c0 2.21-1.79 4-4 4s-4-1.79-4-4V5a2.5 2.5 0 0 1 5 0v10.5c0 .55-.45 1-1 1s-1-.45-1-1V6H10v9.5a2.5 2.5 0 0 0 5 0V5c0-1.38-1.12-2.5-2.5-2.5S10 3.62 10 5v12.5a4 4 0 0 0 8 0V6h-1.5z"/></svg>',
            tooltip: true
        });

        view.on('execute', () => {
            const command = this.editor.commands.get('insertContentFile');
            if (command.isEnabled) {
                fileInput.click();
            }
        });

        return view;
    }
}

export class ContentFilePlugin extends Plugin {
    static get requires() {
        return [ContentFileEditing, ContentFileUI];
    }
}
