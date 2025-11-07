// DOM Elements
const uploadArea = document.getElementById('uploadArea');
const fileInput = document.getElementById('fileInput');

// Database environment toggle function
function toggleDatabaseConfig() {
    const environment = document.getElementById('dbEnvironment').value;
    document.getElementById('localDbConfig').style.display = environment === 'local' ? 'block' : 'none';
    document.getElementById('productionDbConfig').style.display = environment === 'production' ? 'block' : 'none';
}
const fileInfo = document.getElementById('fileInfo');
const fileName = document.getElementById('fileName');
const uploadForm = document.getElementById('uploadForm');
const resultCard = document.getElementById('resultCard');
const resultContent = document.getElementById('resultContent');
const dataCard = document.getElementById('dataCard');
const dataTable = document.getElementById('dataTable');
const loading = document.querySelector('.loading');
const viewDataBtn = document.getElementById('viewDataBtn');
const saveConfigBtn = document.getElementById('saveConfigBtn');
const loadConfigBtn = document.getElementById('loadConfigBtn');
const testConnectionBtn = document.getElementById('testConnectionBtn');
const loadDatabasesBtn = document.getElementById('loadDatabasesBtn');
const databaseSelect = document.getElementById('databaseSelect');
const removeFileBtn = document.getElementById('removeFileBtn');
const browseBtn = document.getElementById('browseBtn');

// File upload handling
uploadArea.addEventListener('click', () => {
    if (!fileInput.files.length) {
        fileInput.click();
    }
});
browseBtn.addEventListener('click', () => fileInput.click());
uploadArea.addEventListener('dragover', handleDragOver);
uploadArea.addEventListener('dragleave', handleDragLeave);
uploadArea.addEventListener('drop', handleDrop);
fileInput.addEventListener('change', handleFileSelect);
removeFileBtn.addEventListener('click', removeFile);

function handleDragOver(e) {
    e.preventDefault();
    uploadArea.classList.add('dragover');
}

function handleDragLeave(e) {
    e.preventDefault();
    uploadArea.classList.remove('dragover');
}

function handleDrop(e) {
    e.preventDefault();
    uploadArea.classList.remove('dragover');
    const files = e.dataTransfer.files;
    if (files.length > 0) {
        // Create new FileList with single file
        const dt = new DataTransfer();
        dt.items.add(files[0]);
        fileInput.files = dt.files;
        showFileInfo(files[0]);
    }
}

function handleFileSelect(e) {
    if (e.target.files.length > 0) {
        showFileInfo(e.target.files[0]);
    } else {
        hideFileInfo();
    }
}

function showFileInfo(file) {
    fileName.textContent = `${file.name} (${formatFileSize(file.size)})`;
    fileInfo.classList.remove('d-none');
}

function hideFileInfo() {
    fileInfo.classList.add('d-none');
    fileName.textContent = '';
}

function removeFile() {
    fileInput.value = '';
    hideFileInfo();
    uploadArea.classList.remove('dragover');
}

function formatFileSize(bytes) {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
}

// Form submission
uploadForm.addEventListener('submit', async (e) => {
    e.preventDefault();
    
    const databaseName = document.getElementById('databaseName').value;
    const tableName = document.getElementById('tableName').value;
    const file = fileInput.files[0];
    
    if (!file) {
        alert('Please select a file');
        return;
    }
    
    const formData = new FormData();
    formData.append('databaseName', databaseName);
    formData.append('tableName', tableName);
    formData.append('file', file);
    
    showLoading(true);
    hideResults();
    
    try {
        const response = await fetch('/api/DynamicImport/upload', {
            method: 'POST',
            body: formData
        });
        
        const result = await response.json();
        showResults(result, response.ok);
        
    } catch (error) {
        showResults({ errors: [error.message] }, false);
    } finally {
        showLoading(false);
    }
});

// Configuration buttons
saveConfigBtn.addEventListener('click', async () => {
    const config = {
        host: document.getElementById('dbHost').value,
        port: document.getElementById('dbPort').value,
        username: document.getElementById('dbUsername').value,
        password: document.getElementById('dbPassword').value,
        database: document.getElementById('databaseName').value
    };
    
    try {
        const response = await fetch('/api/Config/connection', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(config)
        });
        
        const result = await response.json();
        
        if (response.ok) {
            alert('Database configuration saved successfully!');
        } else {
            alert('Error: ' + result.error);
        }
    } catch (error) {
        alert('Error: ' + error.message);
    }
});

loadConfigBtn.addEventListener('click', async () => {
    try {
        const response = await fetch('/api/Config/connection');
        const config = await response.json();
        
        if (response.ok) {
            document.getElementById('dbHost').value = config.host;
            document.getElementById('dbPort').value = config.port;
            document.getElementById('dbUsername').value = config.username;
            document.getElementById('databaseName').value = config.database;
            alert('Configuration loaded successfully!');
        }
    } catch (error) {
        alert('Error loading configuration: ' + error.message);
    }
});

testConnectionBtn.addEventListener('click', async () => {
    const environment = document.getElementById('dbEnvironment').value;
    const config = environment === 'production' ? {
        host: document.getElementById('prodDbHost').value || document.getElementById('dbHost').value,
        port: document.getElementById('prodDbPort').value || document.getElementById('dbPort').value,
        username: document.getElementById('prodDbUsername').value || document.getElementById('dbUsername').value,
        password: document.getElementById('prodDbPassword').value || document.getElementById('dbPassword').value,
        database: 'postgres',
        environment: 'production'
    } : {
        host: document.getElementById('dbHost').value,
        port: document.getElementById('dbPort').value,
        username: document.getElementById('dbUsername').value,
        password: document.getElementById('dbPassword').value,
        database: 'postgres',
        environment: 'local'
    };
    
    try {
        const response = await fetch('/api/Config/test', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(config)
        });
        
        const result = await response.json();
        
        if (result.success) {
            alert('✅ Connection successful!');
        } else {
            alert('❌ Connection failed: ' + result.message);
        }
    } catch (error) {
        alert('❌ Error: ' + error.message);
    }
});

loadDatabasesBtn.addEventListener('click', async () => {
    const host = document.getElementById('dbHost').value;
    const port = document.getElementById('dbPort').value;
    const username = document.getElementById('dbUsername').value;
    const password = document.getElementById('dbPassword').value;
    
    try {
        const response = await fetch(`/api/Config/databases?host=${host}&username=${username}&password=${password}&port=${port}`);
        const databases = await response.json();
        
        if (response.ok) {
            databaseSelect.innerHTML = '<option value="">Select Database...</option>';
            databases.forEach(db => {
                const option = document.createElement('option');
                option.value = db;
                option.textContent = db;
                databaseSelect.appendChild(option);
            });
            databaseSelect.style.display = 'block';
            
            databaseSelect.addEventListener('change', (e) => {
                if (e.target.value) {
                    document.getElementById('databaseName').value = e.target.value;
                }
            });
        } else {
            alert('Error loading databases: ' + databases.error);
        }
    } catch (error) {
        alert('Error: ' + error.message);
    }
});

// View data button
viewDataBtn.addEventListener('click', async () => {
    const databaseName = document.getElementById('databaseName').value;
    const tableName = document.getElementById('tableName').value;
    
    if (!databaseName || !tableName) {
        alert('Please enter database and table name');
        return;
    }
    
    showLoading(true);
    hideResults();
    
    try {
        const response = await fetch(`/api/DynamicImport/data?databaseName=${databaseName}&tableName=${tableName}`);
        const data = await response.json();
        
        if (response.ok) {
            showTableData(data);
        } else {
            showResults({ errors: [data.message || 'Failed to fetch data'] }, false);
        }
        
    } catch (error) {
        showResults({ errors: [error.message] }, false);
    } finally {
        showLoading(false);
    }
});

function showLoading(show) {
    loading.style.display = show ? 'block' : 'none';
}

function hideResults() {
    resultCard.style.display = 'none';
    dataCard.style.display = 'none';
}

function showResults(result, success) {
    let html = '';
    
    if (success) {
        const successRate = result.totalRecords > 0 ? Math.round((result.successfulImports / result.totalRecords) * 100) : 0;
        
        html = `
            <div class="row">
                <div class="col-md-3">
                    <div class="card bg-primary text-white">
                        <div class="card-body text-center">
                            <h4>${result.totalRecords}</h4>
                            <small>Total Records</small>
                        </div>
                    </div>
                </div>
                <div class="col-md-3">
                    <div class="card bg-success text-white">
                        <div class="card-body text-center">
                            <h4>${result.successfulImports}</h4>
                            <small>Successful (${successRate}%)</small>
                        </div>
                    </div>
                </div>
                <div class="col-md-3">
                    <div class="card bg-danger text-white">
                        <div class="card-body text-center">
                            <h4>${result.failedImports}</h4>
                            <small>Failed</small>
                        </div>
                    </div>
                </div>
                <div class="col-md-3">
                    <div class="card bg-info text-white">
                        <div class="card-body text-center">
                            <h4>${result.detectedColumns?.length || 0}</h4>
                            <small>Columns</small>
                        </div>
                    </div>
                </div>
            </div>
        `;
        
        if (result.detectedColumns?.length > 0) {
            html += `
                <div class="mt-3">
                    <h6><i class="fas fa-columns"></i> Detected Columns:</h6>
                    <div class="d-flex flex-wrap gap-2">
                        ${result.detectedColumns.map(col => `<span class="badge bg-secondary">${col}</span>`).join('')}
                    </div>
                </div>
            `;
        }
        
        if (result.errors?.length > 0) {
            const errorMessages = result.errors.filter(e => e.includes('❌'));
            const warningMessages = result.errors.filter(e => e.includes('⚠️'));
            const infoMessages = result.errors.filter(e => e.includes('✅') || e.includes('📋'));
            const otherMessages = result.errors.filter(e => !e.includes('❌') && !e.includes('⚠️') && !e.includes('✅') && !e.includes('📋'));
            
            if (infoMessages.length > 0) {
                html += `
                    <div class="mt-3">
                        <h6><i class="fas fa-info-circle"></i> Information:</h6>
                        <div class="alert alert-info">
                            ${infoMessages.map(msg => `<div>${msg}</div>`).join('')}
                        </div>
                    </div>
                `;
            }
            
            if (warningMessages.length > 0) {
                html += `
                    <div class="mt-3">
                        <h6><i class="fas fa-exclamation-triangle"></i> Warnings:</h6>
                        <div class="alert alert-warning">
                            ${warningMessages.map(msg => `<div>${msg}</div>`).join('')}
                        </div>
                    </div>
                `;
            }
            
            if (errorMessages.length > 0 || otherMessages.length > 0) {
                html += `
                    <div class="mt-3">
                        <h6><i class="fas fa-exclamation-circle"></i> Issues:</h6>
                        <div class="alert alert-danger">
                            ${[...errorMessages, ...otherMessages].map(msg => `<div>${msg}</div>`).join('')}
                        </div>
                    </div>
                `;
            }
        }
        
        if (result.successfulImports > 0) {
            html += `
                <div class="mt-3">
                    <button class="btn btn-success" onclick="viewImportedData()">
                        <i class="fas fa-eye"></i> View Imported Data
                    </button>
                </div>
            `;
        }
    } else {
        html = `
            <div class="alert alert-danger">
                <h6><i class="fas fa-times-circle"></i> Import Failed</h6>
                ${result.errors?.map(error => `<div>• ${error}</div>`).join('') || 'Unknown error occurred'}
            </div>
        `;
    }
    
    resultContent.innerHTML = html;
    resultCard.style.display = 'block';
}

function viewImportedData() {
    viewDataBtn.click();
}

function showTableData(data) {
    if (!data || data.length === 0) {
        dataTable.innerHTML = '<tbody><tr><td class="text-center">No data found</td></tr></tbody>';
        dataCard.style.display = 'block';
        return;
    }
    
    // Create table headers
    const headers = Object.keys(data[0]);
    const headerHtml = `
        <thead class="table-dark">
            <tr>
                ${headers.map(header => `<th>${header}</th>`).join('')}
            </tr>
        </thead>
    `;
    
    // Create table rows
    const rowsHtml = `
        <tbody>
            ${data.map(row => `
                <tr>
                    ${headers.map(header => `<td>${formatCellValue(row[header])}</td>`).join('')}
                </tr>
            `).join('')}
        </tbody>
    `;
    
    dataTable.innerHTML = headerHtml + rowsHtml;
    dataCard.style.display = 'block';
}

function formatCellValue(value) {
    if (value === null || value === undefined) return '';
    if (typeof value === 'string' && value.includes('T')) {
        // Try to format as date
        const date = new Date(value);
        if (!isNaN(date.getTime())) {
            return date.toLocaleString();
        }
    }
    return value.toString();
}