const API_BASE_URL = window.location.origin;

let currentUser = null;
let authToken = null;
let refreshToken = null;
let currentUserIdForDetails = null;
let userDetailsCache = null; // Cache para dados do usuário atual

// Função para fazer requisições com refresh automático de token
async function fetchWithAuth(url, options = {}) {
    const defaultOptions = {
        headers: {
            'Authorization': `Bearer ${authToken}`,
            'Content-Type': 'application/json',
            ...options.headers
        }
    };

    let response = await fetch(url, { ...options, headers: defaultOptions.headers });

    // Se receber 401, tenta refresh do token
    if (response.status === 401 && refreshToken) {
        console.log('Token expirado, tentando refresh...');
        try {
            const refreshResponse = await fetch(`${API_BASE_URL}/api/auth/refresh-token`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ refreshToken })
            });

            if (refreshResponse.ok) {
                const data = await refreshResponse.json();
                authToken = data.token;
                refreshToken = data.refreshToken;
                localStorage.setItem('authToken', authToken);
                localStorage.setItem('refreshToken', refreshToken);

                // Tenta novamente a requisição original
                defaultOptions.headers['Authorization'] = `Bearer ${authToken}`;
                response = await fetch(url, { ...options, headers: defaultOptions.headers });
            } else {
                // Se refresh falhar, faz logout
                console.error('Refresh token falhou, fazendo logout...');
                logout();
                throw new Error('Sessão expirada. Por favor, faça login novamente.');
            }
        } catch (error) {
            console.error('Erro ao fazer refresh do token:', error);
            logout();
            throw error;
        }
    }

    return response;
}

// Initialize
document.addEventListener('DOMContentLoaded', () => {
    checkAuth();
    setupTabs();
    setupForms();
    setupModal();
    loadUserLicenses();
    
    // Verificar se há código Ewelink na URL (caso o callback tenha redirecionado para cá)
    checkEwelinkCallbackFromUrl();
});

// Verificar se há código Ewelink na URL ou sessionStorage
function checkEwelinkCallbackFromUrl() {
    const urlParams = new URLSearchParams(window.location.search);
    const ewelinkCode = urlParams.get('ewelink_code');
    const code = urlParams.get('code');
    const ewelinkProcess = urlParams.get('ewelink_process');
    
    // Verificar sessionStorage primeiro (mais rápido, vem do callback)
    const sessionCode = sessionStorage.getItem('ewelink_oauth_code');
    const sessionRegion = sessionStorage.getItem('ewelink_oauth_region');
    if (sessionCode) {
        const timestamp = parseInt(sessionStorage.getItem('ewelink_oauth_timestamp') || '0');
        const now = Date.now();
        // Verificar se o código não expirou (30 segundos = 30000ms)
        if (now - timestamp < 30000) {
            // Limpar sessionStorage
            sessionStorage.removeItem('ewelink_oauth_code');
            sessionStorage.removeItem('ewelink_oauth_state');
            sessionStorage.removeItem('ewelink_oauth_region');
            sessionStorage.removeItem('ewelink_oauth_timestamp');
            
            // Limpar URL
            window.history.replaceState({}, document.title, window.location.pathname);
            
            // Processar código IMEDIATAMENTE com região
            console.log('Processando código do sessionStorage (rápido), região:', sessionRegion);
            processEwelinkLogin(sessionCode, sessionRegion);
            return;
        } else {
            // Código expirado
            sessionStorage.removeItem('ewelink_oauth_code');
            sessionStorage.removeItem('ewelink_oauth_state');
            sessionStorage.removeItem('ewelink_oauth_region');
            sessionStorage.removeItem('ewelink_oauth_timestamp');
            showNotification('Código de autorização expirado. Por favor, tente novamente.', 'error');
        }
    }
    
    // Processar código da URL se houver
    const region = urlParams.get('region');
    if (ewelinkCode) {
        // Limpar URL primeiro para evitar processamento duplicado
        window.history.replaceState({}, document.title, window.location.pathname);
        // Processar código IMEDIATAMENTE com região
        processEwelinkLogin(ewelinkCode, region);
    } else if (code) {
        // Se vier diretamente com 'code' (fallback)
        window.history.replaceState({}, document.title, window.location.pathname);
        processEwelinkLogin(code, region);
    } else if (ewelinkProcess) {
        // Se veio do callback mas não há código no sessionStorage (pode ter expirado)
        window.history.replaceState({}, document.title, window.location.pathname);
        showNotification('Código de autorização não encontrado ou expirado. Por favor, tente novamente.', 'error');
    }
}

// Processar login Ewelink
async function processEwelinkLogin(code, region = null) {
    try {
        console.log('Processando login Ewelink com código:', code?.substring(0, 20) + '...');
        console.log('Código completo:', code);
        console.log('Região:', region || 'não especificada (usando padrão: as)');
        console.log('Timestamp atual:', Date.now());
        
        // Processar IMEDIATAMENTE - o código expira em 30 segundos
        const requestBody = { code: code };
        if (region) {
            requestBody.region = region;
        }
        
        const response = await fetchWithAuth(`${API_BASE_URL}/api/Ewelink/login`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(requestBody)
        });
        
        if (!response.ok) {
            // Tentar ler como JSON, se falhar, usar texto
            let errorMessage = 'Erro ao fazer login';
            try {
                const errorData = await response.json();
                errorMessage = errorData.message || errorData.title || errorData.error || errorMessage;
                console.error('Erro detalhado:', errorData);
            } catch (e) {
                // Se não for JSON, ler como texto
                const errorText = await response.text();
                errorMessage = errorText || errorMessage;
                console.error('Resposta de erro não é JSON:', errorText);
            }
            throw new Error(errorMessage);
        }
        
        const data = await response.json();
        showNotification('Login realizado com sucesso! Sincronizando dispositivos...', 'success');
        
        // Ativar tab Ewelink e atualizar status
        const ewelinkTabBtn = document.querySelector('[data-tab="ewelink"]');
        if (ewelinkTabBtn) {
            ewelinkTabBtn.click(); // Ativar a tab Ewelink
        }
        
        // Aguardar um pouco para a tab ser ativada e então atualizar status
        // A sincronização já foi feita no backend durante o login, então aguardar um pouco mais
        setTimeout(async () => {
            await checkEwelinkStatus();
            // Aguardar mais um pouco e recarregar dispositivos novamente para garantir
            setTimeout(async () => {
                await loadEwelinkDevices();
            }, 1000);
        }, 500);
    } catch (error) {
        console.error('Erro ao fazer login Ewelink:', error);
        showNotification('Erro ao fazer login: ' + error.message, 'error');
    }
}

// Check if user is already logged in
function checkAuth() {
    const token = localStorage.getItem('authToken');
    const user = localStorage.getItem('currentUser');
    
    if (token && user) {
        authToken = token;
        refreshToken = localStorage.getItem('refreshToken');
        currentUser = JSON.parse(user);
        showDashboard();
        // Carregar notificações se for admin (async, não bloqueia)
        if (currentUser && (currentUser.role === 'Administrador' || currentUser.role === 'userAdmin')) {
            loadNotifications().then(() => {
                if (notificationsInterval) {
                    clearInterval(notificationsInterval);
                }
                notificationsInterval = setInterval(loadNotifications, 30000);
            });
        }
    }
}

// Tab switching
function setupTabs() {
    const tabButtons = document.querySelectorAll('.tab-btn');
    const forms = document.querySelectorAll('.auth-form');

    tabButtons.forEach(btn => {
        btn.addEventListener('click', () => {
            const tab = btn.dataset.tab;
            
            tabButtons.forEach(b => b.classList.remove('active'));
            forms.forEach(f => f.classList.remove('active'));
            
            btn.classList.add('active');
            document.getElementById(`${tab}-form`).classList.add('active');
            
            // Clear errors
            document.querySelectorAll('.error-message').forEach(el => {
                el.classList.remove('show');
                el.textContent = '';
            });
        });
    });
}

// Setup forms
function setupForms() {
    const loginForm = document.getElementById('loginForm');
    const registerForm = document.getElementById('registerForm');

    loginForm.addEventListener('submit', handleLogin);
    registerForm.addEventListener('submit', handleRegister);
}

// Handle Login
async function handleLogin(e) {
    e.preventDefault();
    const errorDiv = document.getElementById('login-error');
    errorDiv.classList.remove('show');

    const email = document.getElementById('login-email').value;
    const password = document.getElementById('login-password').value;

    try {
        const response = await fetch(`${API_BASE_URL}/api/auth/login`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                email,
                password,
                origem: 'web'
            })
        });

        const data = await response.json();

        if (!response.ok) {
            throw new Error(data || 'Erro ao fazer login');
        }

        authToken = data.token;
        refreshToken = data.refreshToken;
        currentUser = data.user;

        localStorage.setItem('authToken', authToken);
        localStorage.setItem('refreshToken', refreshToken);
        localStorage.setItem('currentUser', JSON.stringify(currentUser));

        showDashboard();
        // Carregar notificações se for admin
        if (currentUser && (currentUser.role === 'Administrador' || currentUser.role === 'userAdmin')) {
            loadNotifications().then(() => {
                if (notificationsInterval) {
                    clearInterval(notificationsInterval);
                }
                notificationsInterval = setInterval(loadNotifications, 30000);
            });
        }
    } catch (error) {
        errorDiv.textContent = error.message;
        errorDiv.classList.add('show');
    }
}

// Handle Register
async function handleRegister(e) {
    e.preventDefault();
    const errorDiv = document.getElementById('register-error');
    errorDiv.classList.remove('show');

    const name = document.getElementById('register-name').value;
    const email = document.getElementById('register-email').value;
    const password = document.getElementById('register-password').value;

    try {
        const response = await fetch(`${API_BASE_URL}/api/auth/register`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                name,
                email,
                password,
                origem: 'web'
            })
        });

        const data = await response.json();

        if (!response.ok) {
            throw new Error(data || 'Erro ao cadastrar');
        }

        authToken = data.token;
        refreshToken = data.refreshToken;
        currentUser = data.user;

        localStorage.setItem('authToken', authToken);
        localStorage.setItem('refreshToken', refreshToken);
        localStorage.setItem('currentUser', JSON.stringify(currentUser));

        showDashboard();
    } catch (error) {
        errorDiv.textContent = error.message;
        errorDiv.classList.add('show');
    }
}

// Show Dashboard
async function showDashboard() {
    document.getElementById('auth-section').style.display = 'none';
    document.getElementById('dashboard-section').style.display = 'block';
    
    // Verificar role e carregar notificações se necessário
    if (currentUser) {
        await checkUserRole();
    }
    
    updateAuthMenu();
    
    // Check user role
    checkUserRole();
}

// Sistema de Notificações
let notificationsInterval = null;

async function loadNotifications() {
    const notificationsContainer = document.getElementById('notifications-container');
    if (!notificationsContainer) {
        console.warn('Container de notificações não encontrado');
        return;
    }
    
    if (!currentUser && !authToken) {
        console.log('Usuário não autenticado, ocultando notificações');
        notificationsContainer.style.display = 'none';
        return;
    }
    
    // Verificar se é administrador - primeiro tenta pelo currentUser, depois pela API
    let isAdmin = false;
    if (currentUser && (currentUser.role === 'Administrador' || currentUser.role === 'userAdmin')) {
        isAdmin = true;
    } else {
        // Se não tem role no currentUser, verifica pela API
        try {
            const roleResponse = await fetchWithAuth(`${API_BASE_URL}/api/users/nivel`);
            if (roleResponse.ok) {
                const roleData = await roleResponse.json();
                const role = roleData.nivel;
                isAdmin = role === 'Administrador' || role === 'userAdmin';
                // Atualizar currentUser com o role
                if (currentUser) {
                    currentUser.role = role;
                }
            }
        } catch (error) {
            console.error('Erro ao verificar role:', error);
            notificationsContainer.style.display = 'none';
            return;
        }
    }
    
    console.log('Verificando permissões de notificação:', { isAdmin });
    
    if (!isAdmin) {
        notificationsContainer.style.display = 'none';
        return;
    }
    
    // Mostrar container
    notificationsContainer.style.display = 'flex';
    console.log('Container de notificações exibido');
    
    try {
        const response = await fetchWithAuth(`${API_BASE_URL}/api/notifications/unread-count`);
        if (response.ok) {
            const data = await response.json();
            const count = data.count || 0;
            const badge = document.getElementById('notifications-count');
            if (badge) {
                if (count > 0) {
                    badge.textContent = count > 99 ? '99+' : count;
                    badge.style.display = 'flex';
                } else {
                    badge.style.display = 'none';
                }
            }
        } else if (response.status === 401 || response.status === 403) {
            console.warn('Não autorizado para ver notificações');
            notificationsContainer.style.display = 'none';
        }
    } catch (error) {
        console.error('Erro ao carregar contador de notificações:', error);
        // Mesmo com erro, manter o container visível se for admin
    }
}

async function loadNotificationsList() {
    const notificationsList = document.getElementById('notifications-list');
    if (!notificationsList) return;
    
    notificationsList.innerHTML = '<div class="notification-loading">Carregando notificações...</div>';
    
    try {
        const response = await fetchWithAuth(`${API_BASE_URL}/api/notifications`);
        if (response.ok) {
            const notifications = await response.json();
            
            if (notifications.length === 0) {
                notificationsList.innerHTML = '<div class="notification-empty">Nenhuma notificação</div>';
                return;
            }
            
            const htmlContent = notifications.map(notif => {
                const tipoClass = notif.tipo || notif.Tipo || '';
                const tipoLabel = tipoClass === 'pagamento_avulso' ? 'Adição de Fundos' :
                                 tipoClass === 'assinatura' ? 'Assinatura' :
                                 tipoClass === 'licenca' ? 'Licença' : tipoClass;
                
                const valor = notif.valor || notif.Valor;
                const valorStr = valor ? `R$ ${parseFloat(valor).toFixed(2)}` : '';
                const userName = notif.userName || notif.UserName || 'Usuário';
                const userEmail = notif.userEmail || notif.UserEmail || '';
                const createdAt = new Date(notif.createdAt || notif.CreatedAt).toLocaleString('pt-BR');
                const lida = notif.lida !== undefined ? notif.lida : notif.Lida;
                
                const notifId = notif.id || notif.Id;
                return `
                    <div class="notification-item ${!lida ? 'unread' : ''}">
                        <div class="notification-content" onclick="markNotificationAsRead('${notifId}')">
                            <div class="notification-title">${notif.titulo || notif.Titulo}</div>
                            <div class="notification-message">${notif.mensagem || notif.Mensagem}</div>
                            <div class="notification-meta">
                                <span class="notification-type ${tipoClass}">${tipoLabel}</span>
                                ${valorStr ? `<span class="notification-value">${valorStr}</span>` : ''}
                            </div>
                            <div class="notification-meta" style="margin-top: 0.5rem; font-size: 0.75rem;">
                                <span>${userName}${userEmail ? ` (${userEmail})` : ''}</span>
                                <span>${createdAt}</span>
                            </div>
                        </div>
                        <button class="notification-remove-btn" onclick="event.stopPropagation(); removeNotification('${notifId}')" aria-label="Remover notificação" title="Remover notificação" style="display: flex !important; background: rgba(239, 68, 68, 0.15) !important; border: 1px solid rgba(239, 68, 68, 0.4) !important; color: #ef4444 !important; padding: 0.25rem 0.5rem !important; margin: 0.5rem !important; min-width: 28px !important; min-height: 28px !important; border-radius: 4px !important; cursor: pointer !important; flex-shrink: 0 !important;">
                            <span class="remove-icon" style="font-size: 1.5rem; line-height: 1;">×</span>
                        </button>
                    </div>
                `;
            }).join('');
            
            console.log('HTML gerado (primeiros 500 chars):', htmlContent.substring(0, 500));
            notificationsList.innerHTML = htmlContent;
            console.log('Notificações renderizadas:', notifications.length);
            
            // Verificar se os botões foram criados
            const removeButtons = document.querySelectorAll('.notification-remove-btn');
            console.log('Botões de remover encontrados:', removeButtons.length);
        } else {
            notificationsList.innerHTML = '<div class="notification-empty">Erro ao carregar notificações</div>';
        }
    } catch (error) {
        console.error('Erro ao carregar notificações:', error);
        notificationsList.innerHTML = '<div class="notification-empty">Erro ao carregar notificações</div>';
    }
}

function toggleNotifications() {
    const dropdown = document.getElementById('notifications-dropdown');
    if (!dropdown) return;
    
    if (dropdown.style.display === 'none' || !dropdown.style.display) {
        dropdown.style.display = 'flex';
        loadNotificationsList();
    } else {
        dropdown.style.display = 'none';
    }
}

async function markNotificationAsRead(notificationId) {
    try {
        const response = await fetchWithAuth(`${API_BASE_URL}/api/notifications/${notificationId}/mark-as-read`, {
            method: 'POST'
        });
        
        if (response.ok) {
            // Recarregar lista e contador
            await loadNotificationsList();
            await loadNotifications();
        }
    } catch (error) {
        console.error('Erro ao marcar notificação como lida:', error);
    }
}

async function markAllAsRead() {
    try {
        const response = await fetchWithAuth(`${API_BASE_URL}/api/notifications/mark-all-as-read`, {
            method: 'POST'
        });
        
        if (response.ok) {
            // Recarregar lista e contador
            await loadNotificationsList();
            await loadNotifications();
        }
    } catch (error) {
        console.error('Erro ao marcar todas como lidas:', error);
    }
}

async function removeNotification(notificationId) {
    if (!confirm('Tem certeza que deseja remover esta notificação?')) {
        return;
    }
    
    try {
        const response = await fetchWithAuth(`${API_BASE_URL}/api/notifications/${notificationId}`, {
            method: 'DELETE'
        });
        
        if (response.ok) {
            // Recarregar lista e contador
            await loadNotificationsList();
            await loadNotifications();
        } else {
            const data = await response.json();
            alert(data.message || 'Erro ao remover notificação');
        }
    } catch (error) {
        console.error('Erro ao remover notificação:', error);
        alert('Erro ao remover notificação');
    }
}

// Fechar dropdown ao clicar fora
document.addEventListener('click', (e) => {
    const container = document.getElementById('notifications-container');
    const dropdown = document.getElementById('notifications-dropdown');
    const btn = document.getElementById('notifications-btn');
    
    if (container && dropdown && btn && 
        !container.contains(e.target) && 
        dropdown.style.display === 'flex') {
        dropdown.style.display = 'none';
    }
});

// Check user role and show appropriate dashboard
async function checkUserRole() {
    try {
        const response = await fetchWithAuth(`${API_BASE_URL}/api/users/nivel`);

        if (!response.ok) {
            throw new Error('Erro ao verificar role');
        }

        const data = await response.json();
        const role = data.nivel;
        console.log('Role do usuário:', role);

        if (role === 'Administrador' || role === 'userAdmin') {
            // Inicializar sistema de notificações
            await loadNotifications();
            // Atualizar notificações a cada 30 segundos
            if (notificationsInterval) {
                clearInterval(notificationsInterval);
            }
            notificationsInterval = setInterval(loadNotifications, 30000);
            console.log('Mostrando dashboard de admin');
            document.getElementById('admin-dashboard').style.display = 'block';
            document.getElementById('user-dashboard').style.display = 'none';
            loadAdminDashboard();
            // Conectar ao WebSocket também para admin
            connectDispositivoEspHub();
        } else {
            console.log('Mostrando dashboard de usuário');
            document.getElementById('admin-dashboard').style.display = 'none';
            document.getElementById('user-dashboard').style.display = 'block';
            loadUserDashboard();
        }
    } catch (error) {
        console.error('Erro ao verificar role:', error);
        // Show user dashboard as fallback
        document.getElementById('admin-dashboard').style.display = 'none';
        document.getElementById('user-dashboard').style.display = 'block';
        loadUserDashboard();
    }
}

// Load Admin Dashboard
async function loadAdminDashboard() {
    await loadStats();
    await loadUsers();
    
    // Aguardar um pouco para garantir que o DOM está pronto
    setTimeout(() => {
        setupAdminTabs();
    }, 100);
    
    // Refresh stats every 30 seconds
    setInterval(loadStats, 30000);
}

// Setup Admin Tabs
function setupAdminTabs() {
    console.log('[setupAdminTabs] Inicializando tabs do admin...');
    
    const tabButtons = document.querySelectorAll('.admin-tab-btn');
    const tabContents = document.querySelectorAll('.admin-tab-content');
    
    // Aplicar estilos inline nos botões para garantir que funcionem
    tabButtons.forEach((btn) => {
        btn.style.padding = '1.2rem 1rem';
        btn.style.background = 'transparent';
        btn.style.border = 'none';
        btn.style.color = '#ffffff';
        btn.style.fontSize = '1.3rem';
        btn.style.fontWeight = 'bold';
        btn.style.cursor = 'pointer';
        btn.style.position = 'relative';
        btn.style.whiteSpace = 'nowrap';
        btn.style.margin = '0';
        btn.style.textDecoration = 'none';
        btn.style.outline = 'none';
        btn.style.boxShadow = 'none';
        btn.style.lineHeight = '1.5';
    });
    
    // Remover separadores existentes que possam estar dentro dos botões
    tabButtons.forEach(btn => {
        const separatorInside = btn.querySelector('.tab-separator');
        if (separatorInside) {
            separatorInside.remove();
        }
    });
    
    // Garantir que apenas a primeira tab está ativa inicialmente
    tabContents.forEach((content, index) => {
        if (index === 0) {
            content.classList.add('active');
            content.style.display = 'block';
            content.style.visibility = 'visible';
            content.style.opacity = '1';
        } else {
            content.classList.remove('active');
            content.style.display = 'none';
            content.style.visibility = 'hidden';
            content.style.opacity = '0';
        }
    });
    
    // Garantir que apenas o primeiro botão está ativo e aplicar cor
    tabButtons.forEach((btn, index) => {
        if (index === 0) {
            btn.classList.add('active');
            btn.style.color = 'var(--primary-color)';
        } else {
            btn.classList.remove('active');
            btn.style.color = '#ffffff';
        }
    });
    
    // Adicionar event listeners
    tabButtons.forEach((btn) => {
        // Remover listeners antigos clonando o botão
        const newBtn = btn.cloneNode(true);
        btn.parentNode.replaceChild(newBtn, btn);
        
        // Reaplicar estilos inline
        newBtn.style.padding = '1.2rem 1rem';
        newBtn.style.background = 'transparent';
        newBtn.style.border = 'none';
        newBtn.style.color = newBtn.classList.contains('active') ? 'var(--primary-color)' : '#ffffff';
        newBtn.style.fontSize = '1.3rem';
        newBtn.style.fontWeight = 'bold';
        newBtn.style.cursor = 'pointer';
        newBtn.style.position = 'relative';
        newBtn.style.whiteSpace = 'nowrap';
        newBtn.style.margin = '0';
        newBtn.style.textDecoration = 'none';
        newBtn.style.outline = 'none';
        newBtn.style.boxShadow = 'none';
        newBtn.style.lineHeight = '1.5';
        
        newBtn.addEventListener('click', function(e) {
            e.preventDefault();
            e.stopPropagation();
            const tab = this.dataset.tab;
            console.log('[setupAdminTabs] Tab clicada:', tab);
            
            // Remove active from all tabs and contents
            document.querySelectorAll('.admin-tab-btn').forEach(b => {
                b.classList.remove('active');
                b.style.color = '#ffffff';
            });
            document.querySelectorAll('.admin-tab-content').forEach(c => {
                c.classList.remove('active');
                c.style.display = 'none';
                c.style.visibility = 'hidden';
                c.style.opacity = '0';
            });
            
            // Add active to clicked tab
            this.classList.add('active');
            this.style.color = 'var(--primary-color)';
            const targetContent = document.getElementById(`${tab}-tab`);
            if (targetContent) {
                targetContent.classList.add('active');
                targetContent.style.display = 'block';
                targetContent.style.visibility = 'visible';
                targetContent.style.opacity = '1';
                console.log('[setupAdminTabs] Tab ativada:', tab);
            } else {
                console.error('[setupAdminTabs] Conteúdo da tab não encontrado:', `${tab}-tab`);
            }
            
            // Load content when tab is activated
            if (tab === 'admin-online') {
                loadOnlineUsers();
            } else if (tab === 'admin-planos') {
                loadUsersWithPlans();
            } else if (tab === 'admin-vendas') {
                loadStarkcoinsVendas();
            } else if (tab === 'admin-falhas') {
                loadPagamentosFalhas();
            } else if (tab === 'admin-error-logs') {
                loadErrorLogsUsers();
            } else if (tab === 'admin-consultar-codigo') {
                // Tab de consulta de código - não precisa carregar nada inicialmente
            }
        });
        
        // Adicionar hover
        newBtn.addEventListener('mouseenter', function() {
            if (!this.classList.contains('active')) {
                this.style.color = 'var(--primary-color)';
            }
        });
        
        newBtn.addEventListener('mouseleave', function() {
            if (!this.classList.contains('active')) {
                this.style.color = '#ffffff';
            }
        });
    });
    
    // Adicionar separadores DEPOIS de configurar os event listeners
    const adminTabs = document.querySelector('.admin-tabs');
    if (adminTabs) {
        // Remover separadores existentes
        adminTabs.querySelectorAll('.tab-separator').forEach(sep => sep.remove());
        
        // Buscar os botões novamente (já que foram clonados)
        const currentButtons = adminTabs.querySelectorAll('.admin-tab-btn');
        const buttonsArray = Array.from(currentButtons);
        
        // Adicionar separadores entre os botões (como elementos irmãos)
        // Mas não adicionar após cada 3 tabs (para quebrar linha)
        buttonsArray.forEach((btn, index) => {
            // Não adicionar separador após o 3º, 6º, 9º botão (para quebrar linha)
            if (index < buttonsArray.length - 1 && (index + 1) % 3 !== 0) {
                // Criar separador como elemento irmão
                const separator = document.createElement('span');
                separator.className = 'tab-separator';
                separator.textContent = '|';
                separator.style.color = 'rgba(255, 255, 255, 0.5)';
                separator.style.fontWeight = 'normal';
                separator.style.fontSize = '1.3rem';
                separator.style.padding = '0 0.3rem';
                separator.style.pointerEvents = 'none';
                separator.style.userSelect = 'none';
                separator.style.display = 'inline-block';
                separator.style.verticalAlign = 'middle';
                separator.style.margin = '0';
                
                // Inserir após o botão atual
                btn.insertAdjacentElement('afterend', separator);
            }
        });
        
        // Adicionar quebra de linha após cada 3 tabs
        buttonsArray.forEach((btn, index) => {
            if ((index + 1) % 3 === 0 && index < buttonsArray.length - 1) {
                // Adicionar um elemento de quebra invisível para forçar nova linha
                const lineBreak = document.createElement('div');
                lineBreak.style.flexBasis = '100%';
                lineBreak.style.height = '0';
                lineBreak.style.width = '0';
                lineBreak.style.order = '999';
                lineBreak.style.margin = '0';
                lineBreak.style.padding = '0';
                // Inserir após o separador (se existir) ou após o botão
                const nextSibling = btn.nextSibling;
                if (nextSibling && nextSibling.classList && nextSibling.classList.contains('tab-separator')) {
                    nextSibling.insertAdjacentElement('afterend', lineBreak);
                } else {
                    btn.insertAdjacentElement('afterend', lineBreak);
                }
            }
        });
    }
    
    console.log('[setupAdminTabs] Tabs configuradas. Total de botões:', tabButtons.length, 'Total de conteúdos:', tabContents.length);
}

// Load Stats (Admin only)
async function loadStats() {
    try {
        const response = await fetch(`${API_BASE_URL}/api/admin/stats`, {
            headers: {
                'Authorization': `Bearer ${authToken}`
            }
        });

        if (!response.ok) {
            throw new Error('Erro ao carregar estatísticas');
        }

        const data = await response.json();

        document.getElementById('total-users').textContent = data.totalUsers;
        document.getElementById('active-users').textContent = data.activeUsers;
        document.getElementById('api-status').textContent = data.apiStatus;
        document.getElementById('mqtt-status').textContent = data.mqttStatus;

        // Update status icons
        const apiIcon = document.getElementById('api-status-icon');
        const mqttIcon = document.getElementById('mqtt-status-icon');
        
        if (apiIcon) apiIcon.textContent = data.apiStatus === 'OK' ? '🟢' : '🔴';
        if (mqttIcon) mqttIcon.textContent = data.mqttConnected ? '🟢' : '🔴';
    } catch (error) {
        console.error('Erro ao carregar stats:', error);
    }
}

// Load Users
async function loadUsers() {
    try {
        const response = await fetch(`${API_BASE_URL}/api/admin/users`, {
            headers: {
                'Authorization': `Bearer ${authToken}`
            }
        });

        if (!response.ok) {
            throw new Error('Erro ao carregar usuários');
        }

        const users = await response.json();
        displayUsers(users);
    } catch (error) {
        console.error('Erro ao carregar usuários:', error);
        document.getElementById('users-table-body').innerHTML = 
            '<tr><td colspan="7" class="loading">Erro ao carregar usuários</td></tr>';
    }
}

// Display Users
function displayUsers(users) {
    const tbody = document.getElementById('users-table-body');
    
    if (users.length === 0) {
        tbody.innerHTML = '<tr><td colspan="7" class="loading">Nenhum usuário encontrado</td></tr>';
        return;
    }

    tbody.innerHTML = users.map(user => `
        <tr>
            <td>${user.name}</td>
            <td>${user.email}</td>
            <td><span class="role-badge">${user.role}</span></td>
            <td><span class="status-badge ${user.isActive ? 'active' : 'inactive'}">${user.isActive ? 'Ativo' : 'Inativo'}</span></td>
            <td>${user.starkCoins.toFixed(2)}</td>
            <td>${new Date(user.createdAt).toLocaleDateString('pt-BR')}</td>
            <td>
                <div class="action-buttons">
                    <button class="action-btn view" onclick="viewUserDetails('${user.id}')">Ver Detalhes</button>
                    <button class="action-btn edit" onclick="editUser('${user.id}')">Editar</button>
                    <button class="action-btn delete" onclick="deleteUser('${user.id}')">Deletar</button>
                </div>
            </td>
        </tr>
    `).join('');
}

// Refresh Users
function refreshUsers() {
    loadUsers();
}

// Edit User
async function editUser(userId) {
    try {
        const response = await fetch(`${API_BASE_URL}/api/admin/users/${userId}`, {
            headers: {
                'Authorization': `Bearer ${authToken}`
            }
        });

        if (!response.ok) {
            throw new Error('Erro ao carregar usuário');
        }

        const user = await response.json();
        
        // Garantir que o campo RemovalAds existe antes de preencher
        ensureRemovalAdsField();
        
        document.getElementById('edit-user-id').value = user.id;
        document.getElementById('edit-name').value = user.name;
        document.getElementById('edit-email').value = user.email;
        document.getElementById('edit-role').value = user.role;
        document.getElementById('edit-active').value = user.isActive.toString();
        document.getElementById('edit-coins').value = user.starkCoins;
        
        const removalAdsField = document.getElementById('edit-removal-ads');
        if (removalAdsField) {
            removalAdsField.value = user.removalAds || 'Desativado';
        } else {
            console.error('Campo edit-removal-ads ainda não existe após ensureRemovalAdsField!');
        }
        
        document.getElementById('edit-modal').style.display = 'block';
    } catch (error) {
        alert('Erro ao carregar usuário: ' + error.message);
    }
}

// Delete User
async function deleteUser(userId) {
    if (!confirm('Tem certeza que deseja deletar este usuário?')) {
        return;
    }

    try {
        const response = await fetch(`${API_BASE_URL}/api/admin/users/${userId}`, {
            method: 'DELETE',
            headers: {
                'Authorization': `Bearer ${authToken}`
            }
        });

        if (!response.ok) {
            throw new Error('Erro ao deletar usuário');
        }

        alert('Usuário deletado com sucesso!');
        loadUsers();
        loadStats();
    } catch (error) {
        alert('Erro ao deletar usuário: ' + error.message);
    }
}

// Função para garantir que o campo RemovalAds existe
function ensureRemovalAdsField() {
    const removalAdsField = document.getElementById('edit-removal-ads');
    if (!removalAdsField) {
        console.warn('Campo edit-removal-ads não encontrado! Adicionando dinamicamente...');
        const coinsField = document.getElementById('edit-coins');
        if (coinsField) {
            const coinsGroup = coinsField.closest('.form-group');
            if (coinsGroup) {
                const newField = document.createElement('div');
                newField.className = 'form-group';
                newField.innerHTML = `
                    <label for="edit-removal-ads">RemovalAds</label>
                    <select id="edit-removal-ads" name="removalAds" required>
                        <option value="Desativado">Desativado</option>
                        <option value="Ativo">Ativo</option>
                    </select>
                `;
                coinsGroup.insertAdjacentElement('afterend', newField);
                console.log('Campo RemovalAds adicionado dinamicamente!');
                return true;
            }
        }
        return false;
    }
    return true;
}

// Setup Modal
function setupModal() {
    const modal = document.getElementById('edit-modal');
    const editForm = document.getElementById('edit-user-form');
    
    if (!modal || !editForm) {
        console.error('Modal ou formulário não encontrado!');
        return;
    }

    // Buscar o botão de fechar específico deste modal
    const closeBtn = document.getElementById('close-edit-modal') || modal.querySelector('.close');
    if (closeBtn) {
        // Remover listeners antigos clonando o elemento
        const newCloseBtn = closeBtn.cloneNode(true);
        closeBtn.parentNode.replaceChild(newCloseBtn, closeBtn);
        
        // Adicionar novo listener
        newCloseBtn.addEventListener('click', (e) => {
            e.preventDefault();
            e.stopPropagation();
            console.log('Botão fechar clicado!');
            closeEditModal();
        });
        console.log('Botão de fechar configurado!');
    } else {
        console.error('Botão de fechar não encontrado!');
    }

    // Fechar ao clicar fora do modal
    window.addEventListener('click', (e) => {
        if (e.target === modal) {
            closeEditModal();
        }
    });
    
    // Garantir que o campo RemovalAds existe
    ensureRemovalAdsField();

    editForm.addEventListener('submit', async (e) => {
        e.preventDefault();
        
        const userId = document.getElementById('edit-user-id').value;
        const updateData = {
            name: document.getElementById('edit-name').value,
            email: document.getElementById('edit-email').value,
            role: document.getElementById('edit-role').value,
            isActive: document.getElementById('edit-active').value === 'true',
            starkCoins: parseFloat(document.getElementById('edit-coins').value),
            removalAds: document.getElementById('edit-removal-ads').value
        };

        try {
            const response = await fetch(`${API_BASE_URL}/api/admin/users/${userId}`, {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${authToken}`
                },
                body: JSON.stringify(updateData)
            });

            if (!response.ok) {
                const error = await response.json();
                throw new Error(error.message || 'Erro ao atualizar usuário');
            }

            alert('Usuário atualizado com sucesso!');
            closeEditModal();
            loadUsers();
            loadStats();
        } catch (error) {
            alert('Erro ao atualizar usuário: ' + error.message);
        }
    });
}

function closeEditModal() {
    document.getElementById('edit-modal').style.display = 'none';
}

// View User Details
async function viewUserDetails(userId) {
    currentUserIdForDetails = userId;
    const modal = document.getElementById('user-details-modal');
    const content = document.getElementById('user-details-content');
    
    modal.style.display = 'block';
    content.innerHTML = '<div class="loading-spinner">Carregando detalhes do usuário...</div>';
    
    try {
        const response = await fetch(`${API_BASE_URL}/api/admin/users/${userId}/details`, {
            headers: {
                'Authorization': `Bearer ${authToken}`
            }
        });

        if (!response.ok) {
            throw new Error('Erro ao carregar detalhes do usuário');
        }

        const data = await response.json();
        userDetailsCache = data; // Armazenar cache
        displayUserDetails(data);
    } catch (error) {
        content.innerHTML = `
            <div class="error-message show">
                Erro ao carregar detalhes: ${error.message}
            </div>
        `;
    }
}


// Display User Details
function displayUserDetails(data) {
    const content = document.getElementById('user-details-content');
    
    const formatDate = (dateString) => {
        if (!dateString) return 'N/A';
        return new Date(dateString).toLocaleString('pt-BR');
    };

    content.innerHTML = `
        <!-- User Info Header -->
        <div class="user-info-header">
            <h3>${data.user.name}</h3>
            <div class="user-info-grid">
                <div class="info-box">
                    <div class="info-box-label">Email</div>
                    <div class="info-box-value">${data.user.email}</div>
                </div>
                <div class="info-box">
                    <div class="info-box-label">Role</div>
                    <div class="info-box-value"><span class="role-badge">${data.user.role}</span></div>
                </div>
                <div class="info-box">
                    <div class="info-box-label">Status</div>
                    <div class="info-box-value">
                        <span class="status-badge ${data.user.isActive ? 'active' : 'inactive'}">
                            ${data.user.isActive ? 'Ativo' : 'Inativo'}
                        </span>
                    </div>
                </div>
                <div class="info-box">
                    <div class="info-box-label">StarkCoins</div>
                    <div class="info-box-value">${parseFloat(data.user.starkCoins).toFixed(2)}</div>
                </div>
                <div class="info-box">
                    <div class="info-box-label">Cadastrado em</div>
                    <div class="info-box-value">${formatDate(data.user.createdAt)}</div>
                </div>
            </div>
        </div>

        <!-- Stats Mini -->
        <div class="stats-mini">
            <div class="stat-mini">
                <div class="stat-mini-value">${data.totalDevices}</div>
                <div class="stat-mini-label">Dispositivos</div>
            </div>
            <div class="stat-mini">
                <div class="stat-mini-value">${data.totalComandosSociais}</div>
                <div class="stat-mini-label">Comandos Sociais</div>
            </div>
            <div class="stat-mini">
                <div class="stat-mini-value">${data.totalAgendamentos}</div>
                <div class="stat-mini-label">Agendamentos</div>
            </div>
        </div>

        <!-- Devices Section -->
        <div class="details-section">
            <h4>📱 Dispositivos (${data.devices.length})</h4>
            ${data.devices.length > 0 ? `
                <div class="details-grid">
                    ${data.devices.map(device => `
                        <div class="detail-card">
                            <div style="display: flex; justify-content: space-between; align-items: start; margin-bottom: 1rem;">
                                <h5>${device.name}</h5>
                                <div style="display: flex; gap: 0.5rem;">
                                    <button class="action-btn edit edit-device-btn" data-device-id="${device.id}" style="padding: 0.25rem 0.75rem; font-size: 0.8rem;">Editar</button>
                                    <button class="action-btn delete delete-device-btn" data-device-id="${device.id}" style="padding: 0.25rem 0.75rem; font-size: 0.8rem;">Deletar</button>
                                </div>
                            </div>
                            <p><span class="detail-label">Comando:</span> ${device.comando || 'N/A'}</p>
                            <p><span class="detail-label">MQTT Topic:</span> <code style="font-size: 0.8rem; color: var(--primary-color);">${device.mqttTopic}</code></p>
                            <p><span class="detail-label">API Key:</span> <code style="font-size: 0.8rem; color: var(--light-text);">${device.apiKey.substring(0, 20)}...</code></p>
                            <p><span class="detail-label">Agendamentos:</span> ${device.agendamentosCount}</p>
                        </div>
                    `).join('')}
                </div>
            ` : `
                <div class="empty-state">
                    <div class="empty-state-icon">📱</div>
                    <p>Nenhum dispositivo cadastrado</p>
                </div>
            `}
        </div>

        <!-- Comandos Sociais Section -->
        <div class="details-section">
            <h4>💬 Comandos Sociais (${data.comandosSociais.length})</h4>
            ${data.comandosSociais.length > 0 ? `
                <div class="comandos-list">
                    ${data.comandosSociais.map(cmd => `
                        <div class="comando-item">
                            <div class="comando-item-header">
                                <span class="comando-text">/${cmd.comando}</span>
                                <div style="display: flex; gap: 0.5rem;">
                                    <button class="action-btn edit edit-comando-btn" data-comando-id="${cmd.id}" style="padding: 0.25rem 0.75rem; font-size: 0.8rem;">Editar</button>
                                    <button class="action-btn delete delete-comando-btn" data-comando-id="${cmd.id}" style="padding: 0.25rem 0.75rem; font-size: 0.8rem;">Deletar</button>
                                </div>
                            </div>
                            <div class="resposta-text">
                                <span class="detail-label">Resposta:</span> ${cmd.resposta}
                            </div>
                            ${cmd.respostasAleatorias ? `
                                <div class="resposta-text" style="margin-top: 0.5rem; font-size: 0.85rem; opacity: 0.8;">
                                    <span class="detail-label">Variações:</span> ${cmd.respostasAleatorias.substring(0, 100)}...
                                </div>
                            ` : ''}
                        </div>
                    `).join('')}
                </div>
            ` : `
                <div class="empty-state">
                    <div class="empty-state-icon">💬</div>
                    <p>Nenhum comando social cadastrado</p>
                </div>
            `}
        </div>

        <!-- Agendamentos Section -->
        <div class="details-section">
            <h4>⏰ Agendamentos (${data.agendamentos.length})</h4>
            ${data.agendamentos.length > 0 ? `
                <div class="comandos-list">
                    ${data.agendamentos.map(ag => `
                        <div class="agendamento-item">
                            <div style="display: flex; justify-content: space-between; align-items: start; margin-bottom: 0.5rem;">
                                <h5>${ag.deviceName}</h5>
                                <div style="display: flex; gap: 0.5rem;">
                                    <button class="action-btn edit edit-agendamento-btn" data-agendamento-id="${ag.id}" style="padding: 0.25rem 0.75rem; font-size: 0.8rem;">Editar</button>
                                    <button class="action-btn delete delete-agendamento-btn" data-agendamento-id="${ag.id}" style="padding: 0.25rem 0.75rem; font-size: 0.8rem;">Deletar</button>
                                </div>
                            </div>
                            <p><span class="detail-label">Comando:</span> ${ag.comando}</p>
                            <p><span class="detail-label">Agendado para:</span> ${formatDate(ag.agendadoPara)}</p>
                            ${ag.recorrencia ? `<p><span class="detail-label">Recorrência:</span> ${ag.recorrencia}</p>` : ''}
                            <span class="agendamento-status ${ag.executado ? 'executado' : 'pendente'}">
                                ${ag.executado ? '✓ Executado' : '⏳ Pendente'}
                            </span>
                        </div>
                    `).join('')}
                </div>
            ` : `
                <div class="empty-state">
                    <div class="empty-state-icon">⏰</div>
                    <p>Nenhum agendamento cadastrado</p>
                </div>
            `}
        </div>

        <!-- Último Comando Section -->
        <div class="details-section">
            <h4>🤖 Último Comando de IA</h4>
            ${data.ultimoComando ? `
                <div class="detail-card">
                    <p><span class="detail-label">Usuário disse:</span> ${data.ultimoComando.textoUsuario}</p>
                    <p><span class="detail-label">IA respondeu:</span> ${data.ultimoComando.textoIa}</p>
                    <p style="margin-top: 1rem; font-size: 0.85rem; color: var(--light-text);">
                        <span class="detail-label">Data:</span> ${formatDate(data.ultimoComando.criadoEm)}
                    </p>
                </div>
            ` : `
                <div class="empty-state">
                    <div class="empty-state-icon">🤖</div>
                    <p>Nenhum comando de IA registrado</p>
                </div>
            `}
        </div>
    `;
    
    // Setup event listeners for dynamically created buttons
    setupDetailButtons();
}

// Setup event listeners for detail buttons
function setupDetailButtons() {
    if (!userDetailsCache) {
        console.log('setupDetailButtons: userDetailsCache is null');
        return;
    }
    
    console.log('setupDetailButtons: Setting up event listeners');
    
    // Remove existing listeners first to avoid duplicates
    document.querySelectorAll('.edit-device-btn, .delete-device-btn, .edit-comando-btn, .delete-comando-btn, .edit-agendamento-btn, .delete-agendamento-btn').forEach(btn => {
        const newBtn = btn.cloneNode(true);
        btn.parentNode.replaceChild(newBtn, btn);
    });
    
    // Device buttons - use index-based mapping
    const editDeviceBtns = document.querySelectorAll('.edit-device-btn');
    console.log('Found', editDeviceBtns.length, 'edit-device-btn');
    editDeviceBtns.forEach((btn, index) => {
        if (index >= userDetailsCache.devices.length) return;
        const device = userDetailsCache.devices[index];
        btn.addEventListener('click', (e) => {
            e.preventDefault();
            e.stopPropagation();
            editDevice(device.id, device.name, device.comando || '');
        });
    });
    
    document.querySelectorAll('.delete-device-btn').forEach(btn => {
        btn.addEventListener('click', (e) => {
            e.preventDefault();
            e.stopPropagation();
            const deviceId = btn.getAttribute('data-device-id');
            deleteDevice(deviceId);
        });
    });
    
    // Comando Social buttons - use index-based mapping for reliability
    const editComandoBtns = document.querySelectorAll('.edit-comando-btn');
    console.log('Found', editComandoBtns.length, 'edit-comando-btn');
    console.log('Available comandos:', userDetailsCache.comandosSociais.length);
    
    editComandoBtns.forEach((btn, index) => {
        if (index >= userDetailsCache.comandosSociais.length) {
            console.error('Index out of bounds:', index);
            return;
        }
        
        const cmd = userDetailsCache.comandosSociais[index];
        console.log('Setting up button for comando:', cmd.comando, 'at index', index);
        
        btn.addEventListener('click', (e) => {
            e.preventDefault();
            e.stopPropagation();
            console.log('Edit comando clicked, index:', index, 'comando:', cmd.comando);
            editComandoSocial(cmd.id, cmd.comando, cmd.resposta, cmd.respostasAleatorias || '');
        });
    });
    
    document.querySelectorAll('.delete-comando-btn').forEach(btn => {
        btn.addEventListener('click', (e) => {
            e.preventDefault();
            e.stopPropagation();
            const comandoId = btn.getAttribute('data-comando-id');
            deleteComandoSocial(comandoId);
        });
    });
    
    // Agendamento buttons - use index-based mapping
    document.querySelectorAll('.edit-agendamento-btn').forEach((btn, index) => {
        if (index >= userDetailsCache.agendamentos.length) return;
        const ag = userDetailsCache.agendamentos[index];
        btn.addEventListener('click', (e) => {
            e.preventDefault();
            e.stopPropagation();
            editAgendamento(ag.id, ag.comando, ag.agendadoPara, ag.recorrencia || '', ag.executado);
        });
    });
    
    document.querySelectorAll('.delete-agendamento-btn').forEach(btn => {
        btn.addEventListener('click', (e) => {
            e.preventDefault();
            e.stopPropagation();
            const agendamentoId = btn.getAttribute('data-agendamento-id');
            deleteAgendamento(agendamentoId);
        });
    });
}

function closeUserDetailsModal() {
    document.getElementById('user-details-modal').style.display = 'none';
}

// Close modal when clicking outside
window.addEventListener('click', (e) => {
    const modal = document.getElementById('user-details-modal');
    if (e.target === modal) {
        closeUserDetailsModal();
    }
});

// ========== DEVICE MANAGEMENT ==========
function editDevice(deviceId, name, comando) {
    const modal = document.getElementById('edit-device-modal');
    document.getElementById('edit-device-id').value = deviceId;
    document.getElementById('edit-device-name').value = name || '';
    document.getElementById('edit-device-comando').value = comando || '';
    modal.style.display = 'block';
}

function deleteDevice(deviceId) {
    if (!confirm('Tem certeza que deseja deletar este dispositivo?')) return;

    fetch(`${API_BASE_URL}/api/admin/devices/${deviceId}`, {
        method: 'DELETE',
        headers: { 'Authorization': `Bearer ${authToken}` }
    })
    .then(res => res.json())
    .then(data => {
        alert('Dispositivo deletado com sucesso!');
        if (currentUserIdForDetails) viewUserDetails(currentUserIdForDetails);
    })
    .catch(err => alert('Erro: ' + err.message));
}

// ========== COMANDO SOCIAL MANAGEMENT ==========
function editComandoSocial(comandoId, comando, resposta, respostasAleatorias) {
    console.log('editComandoSocial called:', { comandoId, comando, resposta, respostasAleatorias });
    const modal = document.getElementById('edit-comando-modal');
    if (!modal) {
        console.error('Modal edit-comando-modal not found!');
        return;
    }
    document.getElementById('edit-comando-id').value = comandoId;
    document.getElementById('edit-comando-comando').value = comando || '';
    document.getElementById('edit-comando-resposta').value = resposta || '';
    document.getElementById('edit-comando-variacoes').value = respostasAleatorias || '';
    modal.style.display = 'block';
    console.log('Modal should be visible now');
}

function deleteComandoSocial(comandoId) {
    if (!confirm('Tem certeza que deseja deletar este comando social?')) return;

    fetch(`${API_BASE_URL}/api/admin/comandos-sociais/${comandoId}`, {
        method: 'DELETE',
        headers: { 'Authorization': `Bearer ${authToken}` }
    })
    .then(res => res.json())
    .then(data => {
        alert('Comando social deletado com sucesso!');
        if (currentUserIdForDetails) viewUserDetails(currentUserIdForDetails);
    })
    .catch(err => alert('Erro: ' + err.message));
}

// ========== AGENDAMENTO MANAGEMENT ==========
function editAgendamento(agendamentoId, comando, agendadoPara, recorrencia, executado) {
    const modal = document.getElementById('edit-agendamento-modal');
    document.getElementById('edit-agendamento-id').value = agendamentoId;
    document.getElementById('edit-agendamento-comando').value = comando || '';
    
    // Format date for datetime-local input
    const date = new Date(agendadoPara);
    const localDate = new Date(date.getTime() - date.getTimezoneOffset() * 60000);
    document.getElementById('edit-agendamento-data').value = localDate.toISOString().slice(0, 16);
    
    document.getElementById('edit-agendamento-recorrencia').value = recorrencia || '';
    document.getElementById('edit-agendamento-executado').checked = executado;
    modal.style.display = 'block';
}

function deleteAgendamento(agendamentoId) {
    if (!confirm('Tem certeza que deseja deletar este agendamento?')) return;

    fetch(`${API_BASE_URL}/api/admin/agendamentos/${agendamentoId}`, {
        method: 'DELETE',
        headers: { 'Authorization': `Bearer ${authToken}` }
    })
    .then(res => res.json())
    .then(data => {
        alert('Agendamento deletado com sucesso!');
        if (currentUserIdForDetails) viewUserDetails(currentUserIdForDetails);
    })
    .catch(err => alert('Erro: ' + err.message));
}

// Setup edit forms
document.addEventListener('DOMContentLoaded', () => {
    // Device form
    const editDeviceForm = document.getElementById('edit-device-form');
    if (editDeviceForm) {
        editDeviceForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            const deviceId = document.getElementById('edit-device-id').value;
            const updateData = {
                name: document.getElementById('edit-device-name').value,
                comando: document.getElementById('edit-device-comando').value
            };

            try {
                const response = await fetch(`${API_BASE_URL}/api/admin/devices/${deviceId}`, {
                    method: 'PUT',
                    headers: {
                        'Content-Type': 'application/json',
                        'Authorization': `Bearer ${authToken}`
                    },
                    body: JSON.stringify(updateData)
                });

                if (!response.ok) throw new Error('Erro ao atualizar dispositivo');
                
                alert('Dispositivo atualizado com sucesso!');
                document.getElementById('edit-device-modal').style.display = 'none';
                if (currentUserIdForDetails) viewUserDetails(currentUserIdForDetails);
            } catch (error) {
                alert('Erro: ' + error.message);
            }
        });
    }

    // Comando Social form
    const editComandoForm = document.getElementById('edit-comando-form');
    if (editComandoForm) {
        editComandoForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            const comandoId = document.getElementById('edit-comando-id').value;
            const updateData = {
                comando: document.getElementById('edit-comando-comando').value,
                resposta: document.getElementById('edit-comando-resposta').value,
                respostasAleatorias: document.getElementById('edit-comando-variacoes').value || null
            };

            try {
                const response = await fetch(`${API_BASE_URL}/api/admin/comandos-sociais/${comandoId}`, {
                    method: 'PUT',
                    headers: {
                        'Content-Type': 'application/json',
                        'Authorization': `Bearer ${authToken}`
                    },
                    body: JSON.stringify(updateData)
                });

                if (!response.ok) throw new Error('Erro ao atualizar comando social');
                
                alert('Comando social atualizado com sucesso!');
                document.getElementById('edit-comando-modal').style.display = 'none';
                if (currentUserIdForDetails) viewUserDetails(currentUserIdForDetails);
            } catch (error) {
                alert('Erro: ' + error.message);
            }
        });
    }

    // Agendamento form
    const editAgendamentoForm = document.getElementById('edit-agendamento-form');
    if (editAgendamentoForm) {
        editAgendamentoForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            const agendamentoId = document.getElementById('edit-agendamento-id').value;
            const dataInput = document.getElementById('edit-agendamento-data').value;
            const dataDate = new Date(dataInput);
            
            const updateData = {
                comando: document.getElementById('edit-agendamento-comando').value,
                agendadoPara: dataDate.toISOString(),
                recorrencia: document.getElementById('edit-agendamento-recorrencia').value || null,
                executado: document.getElementById('edit-agendamento-executado').checked
            };

            try {
                const response = await fetch(`${API_BASE_URL}/api/admin/agendamentos/${agendamentoId}`, {
                    method: 'PUT',
                    headers: {
                        'Content-Type': 'application/json',
                        'Authorization': `Bearer ${authToken}`
                    },
                    body: JSON.stringify(updateData)
                });

                if (!response.ok) throw new Error('Erro ao atualizar agendamento');
                
                alert('Agendamento atualizado com sucesso!');
                document.getElementById('edit-agendamento-modal').style.display = 'none';
                if (currentUserIdForDetails) viewUserDetails(currentUserIdForDetails);
            } catch (error) {
                alert('Erro: ' + error.message);
            }
        });
    }
});

// Update Auth Menu
function updateAuthMenu() {
    const authMenuItem = document.getElementById('auth-menu-item');
    if (currentUser) {
        authMenuItem.innerHTML = `
            <li>
                <a href="#" onclick="logout()" style="color: var(--error-color);">Sair</a>
            </li>
        `;
    }
}

// Load User Dashboard
async function loadUserDashboard() {
    console.log('Carregando dashboard de usuário...');
    try {
        await loadUserStats();
        try {
            await loadUserInfo();
            console.log('✅ [Dashboard] loadUserInfo concluído');
            
            // Verificar endereço após carregar info do usuário com sucesso
            await checkAddressAndRedirect();
        } catch (error) {
            console.error('❌ [Dashboard] Erro ao carregar info do usuário:', error);
            // Tentar verificar endereço mesmo com erro (pode ser que o usuário esteja logado mas com erro temporário)
            setTimeout(() => {
                checkAddressAndRedirect();
            }, 2000);
        }
        
        await loadUserDevices();
        console.log('✅ [Dashboard] loadUserDevices concluído');
        try {
        await loadUserComandos();
            console.log('✅ [Dashboard] loadUserComandos concluído');
        } catch (error) {
            console.error('❌ [Dashboard] Erro em loadUserComandos (continuando):', error);
        }
        try {
        await loadUserDispositivosEsp();
            console.log('✅ [Dashboard] loadUserDispositivosEsp concluído');
        } catch (error) {
            console.error('❌ [Dashboard] Erro em loadUserDispositivosEsp (continuando):', error);
        }
        try {
        await loadUserAgendamentos();
            console.log('✅ [Dashboard] loadUserAgendamentos concluído');
        } catch (error) {
            console.error('❌ [Dashboard] Erro em loadUserAgendamentos (continuando):', error);
        }
        
        // Setup user tabs ANTES de carregar planos ativos para garantir que o elemento existe
        console.log('📋 [Dashboard] Configurando tabs...');
        setupUserTabs();
        console.log('✅ [Dashboard] setupUserTabs concluído');
        
        // Setup user forms
        console.log('📋 [Dashboard] Configurando forms...');
        setupUserForms();
        setupDispositivosEspForms();
        setupAgendamentosForms();
        console.log('✅ [Dashboard] Forms configurados');
        
        // Carregar planos ativos após setup das tabs
        console.log('📋 [Dashboard] Chamando loadPlanosAtivos()...');
        console.log('📋 [Dashboard] Tipo de loadPlanosAtivos:', typeof loadPlanosAtivos);
        if (typeof loadPlanosAtivos === 'function') {
            try {
                await loadPlanosAtivos();
                console.log('✅ [Dashboard] loadPlanosAtivos() concluído');
            } catch (error) {
                console.error('❌ [Dashboard] Erro ao chamar loadPlanosAtivos():', error);
                console.error('❌ [Dashboard] Stack:', error.stack);
            }
        } else {
            console.error('❌ [Dashboard] loadPlanosAtivos não é uma função!');
        }
        
        // Conectar ao WebSocket
        connectDispositivoEspHub();
        
        // Refresh stats every 30 seconds
        setInterval(loadUserStats, 30000);
    } catch (error) {
        console.error('Erro ao carregar dashboard de usuário:', error);
    }
}

// Load User Stats
async function loadUserStats() {
    try {
        console.log('Carregando estatísticas do usuário...');
        const response = await fetchWithAuth(`${API_BASE_URL}/api/users/stats`);

        if (!response.ok) {
            const errorText = await response.text();
            console.error('Erro na resposta:', response.status, errorText);
            throw new Error('Erro ao carregar estatísticas');
        }

        const data = await response.json();
        console.log('Estatísticas carregadas:', data);

        const totalDevicesEl = document.getElementById('user-total-devices');
        const totalComandosEl = document.getElementById('user-total-comandos');
        const apiStatusEl = document.getElementById('user-api-status');
        const mqttStatusEl = document.getElementById('user-mqtt-status');

        if (totalDevicesEl) totalDevicesEl.textContent = data.totalDevices || 0;
        if (totalComandosEl) totalComandosEl.textContent = data.totalComandosSociais || 0;
        if (apiStatusEl) apiStatusEl.textContent = data.apiStatus || 'OK';
        if (mqttStatusEl) mqttStatusEl.textContent = data.mqttStatus || 'Desconectado';

        // Update status icons
        const apiIcon = document.getElementById('user-api-status-icon');
        const mqttIcon = document.getElementById('user-mqtt-status-icon');
        
        if (apiIcon) apiIcon.textContent = data.apiStatus === 'OK' ? '🟢' : '🔴';
        if (mqttIcon) mqttIcon.textContent = data.mqttConnected ? '🟢' : '🔴';
    } catch (error) {
        console.error('Erro ao carregar stats do usuário:', error);
    }
}

// Load User Info
async function loadUserInfo() {
    try {
        const response = await fetchWithAuth(`${API_BASE_URL}/api/users/me`);

        if (!response.ok) throw new Error('Erro ao carregar informações do usuário');

        const user = await response.json();

        document.getElementById('user-name').textContent = user.name || '';
        document.getElementById('user-email').textContent = user.email || '';
        document.getElementById('user-coins').textContent = (user.starkCoins || 0).toFixed(2);
        document.getElementById('user-api-key').textContent = user.apiKey || '';
        
        // Armazenar dados do usuário para verificação de endereço
        window.currentUserData = user;
    } catch (error) {
        console.error('Erro ao carregar info do usuário:', error);
    }
}

// Verificar endereço e redirecionar se necessário
async function checkAddressAndRedirect() {
    try {
        const response = await fetchWithAuth(`${API_BASE_URL}/api/users/me`);
        if (!response.ok) {
            // Se houver erro 500, pode ser que a migration não foi aplicada ainda
            // Tentar novamente após um delay maior
            if (response.status === 500) {
                console.warn('⚠️ [Address Check] Erro 500 ao verificar endereço. Pode ser que a migration não foi aplicada.');
                setTimeout(() => {
                    checkAddressAndRedirect();
                }, 3000);
            }
            return;
        }
        
        const user = await response.json();
        
        // Verificar se Estado, Cidade e Bairro estão preenchidos
        if (!user.estado || !user.cidade || !user.bairro) {
            console.log('⚠️ [Address Check] Dados de endereço incompletos. Redirecionando para configurações...');
            
            // Aguardar um pouco para garantir que a UI está pronta
            setTimeout(() => {
                // Abrir tab de configurações
                const configTab = document.querySelector('.user-tab-btn[data-tab="configuracoes"]');
                if (configTab) {
                    console.log('✅ [Address Check] Abrindo tab de configurações...');
                    configTab.click();
                } else {
                    console.error('❌ [Address Check] Tab de configurações não encontrada!');
                }
                
                // Mostrar mensagem e abrir modal
                setTimeout(() => {
                    alert('Insira os dados de endereço para melhor funcionamento do sistema.');
                    // Abrir modal de edição de perfil
                    if (typeof openEditProfileModal === 'function') {
                        openEditProfileModal();
                    } else {
                        console.error('❌ [Address Check] Função openEditProfileModal não encontrada!');
                    }
                }, 800);
            }, 500);
        } else {
            console.log('✅ [Address Check] Dados de endereço completos.');
        }
    } catch (error) {
        console.error('Erro ao verificar endereço:', error);
        // Tentar novamente após um delay se houver erro de rede
        setTimeout(() => {
            checkAddressAndRedirect();
        }, 2000);
    }
}

// Load User Devices
async function loadUserDevices() {
    try {
        const response = await fetchWithAuth(`${API_BASE_URL}/api/devices`);

        if (!response.ok) throw new Error('Erro ao carregar dispositivos');

        const devices = await response.json();
        const devicesList = document.getElementById('devices-list');

        if (devices.length === 0) {
            devicesList.innerHTML = '<div class="empty-state"><div class="empty-state-icon">📱</div><p>Nenhum dispositivo cadastrado</p></div>';
            return;
        }

        devicesList.innerHTML = devices.map(device => `
            <div class="item-card">
                <div class="item-card-header">
                    <div class="item-card-title">${escapeHtml(device.name || 'Sem nome')}</div>
                    <div class="item-card-actions">
                        <button class="btn btn-secondary" onclick="editUserDevice('${device.id}')">Editar</button>
                        <button class="btn btn-secondary" onclick="deleteUserDevice('${device.id}')" style="background: rgba(239, 68, 68, 0.2); color: var(--error-color);">Excluir</button>
                    </div>
                </div>
                <div class="item-card-body">
                    <p><strong>Comando:</strong> ${escapeHtml(device.comando || 'N/A')}</p>
                    <p><strong>MQTT Topic:</strong> <code>${escapeHtml(device.mqttTopic || 'N/A')}</code></p>
                </div>
                <div class="device-actions">
                    <button class="btn btn-starkswit" onclick="acionarDevice('${device.id}', 'ligar')">🔌 Ligar</button>
                    <button class="btn btn-starkswit" onclick="acionarDevice('${device.id}', 'desligar')">🔌 Desligar</button>
                </div>
            </div>
        `).join('');
    } catch (error) {
        console.error('Erro ao carregar dispositivos:', error);
        document.getElementById('devices-list').innerHTML = '<div class="error-message show">Erro ao carregar dispositivos</div>';
    }
}

// Load User Comandos Sociais
async function loadUserComandos() {
    try {
        console.log('Carregando comandos sociais...');
        // Tenta primeiro com ComandosSociais (padrão ASP.NET Core)
        let response = await fetchWithAuth(`${API_BASE_URL}/api/ComandosSociais`);

        // Se 404, tenta com kebab-case
        if (response.status === 404) {
            console.log('Tentando com kebab-case...');
            response = await fetchWithAuth(`${API_BASE_URL}/api/comandos-sociais`);
        }

        if (!response.ok) {
            const errorText = await response.text();
            console.error('Erro na resposta de comandos sociais:', response.status, errorText);
            throw new Error('Erro ao carregar comandos sociais');
        }

        const comandos = await response.json();
        const comandosList = document.getElementById('comandos-list');

        if (comandos.length === 0) {
            comandosList.innerHTML = '<div class="empty-state"><div class="empty-state-icon">💬</div><p>Nenhum comando social cadastrado</p></div>';
            return;
        }

        comandosList.innerHTML = comandos.map(comando => {
            let variacoes = 'N/A';
            try {
                if (comando.respostasAleatorias) {
                    const parsed = JSON.parse(comando.respostasAleatorias);
                    if (parsed.alternativas) {
                        variacoes = parsed.alternativas.join(', ');
                    }
                }
            } catch (e) {
                variacoes = comando.respostasAleatorias || 'N/A';
            }

            return `
                <div class="item-card">
                    <div class="item-card-header">
                        <div class="item-card-title">${escapeHtml(comando.comando || 'Sem comando')}</div>
                        <div class="item-card-actions">
                            <button class="btn btn-secondary" onclick="editUserComando('${comando.id}')">Editar</button>
                            <button class="btn btn-secondary" onclick="deleteUserComando('${comando.id}')" style="background: rgba(239, 68, 68, 0.2); color: var(--error-color);">Excluir</button>
                        </div>
                    </div>
                    <div class="item-card-body">
                        <p><strong>Resposta:</strong> ${escapeHtml(comando.resposta || 'N/A')}</p>
                        <p><strong>Variações:</strong> ${escapeHtml(variacoes)}</p>
                    </div>
                </div>
            `;
        }).join('');
    } catch (error) {
        console.error('Erro ao carregar comandos sociais:', error);
        document.getElementById('comandos-list').innerHTML = '<div class="error-message show">Erro ao carregar comandos sociais</div>';
    }
}

// Setup User Tabs
// Função para ativar uma tab programaticamente
function activateTab(tabName) {
    console.log('🔧 [Tabs] Ativando tab:', tabName);
    
    const tabButtons = document.querySelectorAll('.user-tab-btn');
    const tabContents = document.querySelectorAll('.user-tab-content');
    
    // Ativar botão da tab desktop
    tabButtons.forEach(b => {
        b.classList.remove('active');
        if (b.dataset.tab === tabName) {
            b.classList.add('active');
        }
    });
    
    // Ativar conteúdo da tab
    tabContents.forEach(c => {
        c.classList.remove('active');
    });
    
    const tabContent = document.getElementById(`${tabName}-tab`);
    if (tabContent) {
        tabContent.classList.add('active');
    } else {
        console.error('❌ [Tabs] Tab content não encontrado:', `${tabName}-tab`);
    }
    
    // Atualizar label do toggle em mobile e recolher
    if (window.innerWidth <= 768) {
        const toggle = document.getElementById('user-tabs-toggle');
        const activeLabel = document.getElementById('user-tabs-active-label');
        const container = document.querySelector('.user-tabs-container');
        
        if (toggle && activeLabel) {
            const activeTab = document.querySelector(`.user-tab-btn[data-tab="${tabName}"]`);
            if (activeTab) {
                activeLabel.textContent = activeTab.textContent;
            }
        }
        
        // Recolher menu após seleção
        if (container) {
            container.classList.remove('expanded');
            container.classList.add('collapsed');
        }
    }
    
    // Carregar dados específicos da tab
    loadTabContent(tabName);
}

function loadTabContent(tabName) {
    console.log('🔧 [Tabs] Carregando conteúdo da tab:', tabName);
    
    // Adicionar timeout para garantir que a tab está visível
    setTimeout(() => {
        switch(tabName) {
            case 'planos-ativos':
                console.log('🔧 [Tabs] Carregando planos ativos...');
                if (typeof loadPlanosAtivos === 'function') {
                    loadPlanosAtivos().catch(error => {
                        console.error('❌ [Tabs] Erro ao carregar planos ativos:', error);
                    });
                }
                break;
                
            case 'windows-software':
                console.log('🔧 [Tabs] Carregando licenças...');
                if (typeof loadUserLicenses === 'function') {
                    loadUserLicenses().catch(error => {
                        console.error('❌ [Tabs] Erro ao carregar licenças:', error);
                    });
                }
                break;
                
            case 'ewelink':
                console.log('🔧 [Tabs] Carregando dispositivos Ewelink...');
                if (typeof checkEwelinkStatus === 'function') {
                    checkEwelinkStatus();
                }
                break;
                
            case 'previsao-tempo':
                console.log('🔧 [Tabs] Carregando previsão do tempo...');
                if (typeof loadWeatherForecast === 'function') {
                    loadWeatherForecast();
                }
                break;
        }
    }, 300);
}


function setupUserTabs() {
    console.log('🔧 [Tabs] Configurando tabs...');
    const tabButtons = document.querySelectorAll('.user-tab-btn');
    const tabContents = document.querySelectorAll('.user-tab-content');
    const toggle = document.getElementById('user-tabs-toggle');
    const container = document.querySelector('.user-tabs-container');
    
    console.log('🔧 [Tabs] Encontrados', tabButtons.length, 'botões de tab');
    console.log('🔧 [Tabs] Encontrados', tabContents.length, 'conteúdos de tab');

    // Se não houver tabs, não fazer nada
    if (tabButtons.length === 0) {
        console.warn('⚠️ [Tabs] Nenhuma tab encontrada!');
        return;
    }

    // Configurar botão toggle para mobile
    if (toggle && container) {
        toggle.addEventListener('click', (e) => {
            e.preventDefault();
            e.stopPropagation();
            container.classList.toggle('expanded');
            container.classList.toggle('collapsed');
        });
        
        // Inicializar como collapsed em mobile
        if (window.innerWidth <= 768) {
            container.classList.add('collapsed');
        }
    }

    // Configurar botões de tab
    tabButtons.forEach(btn => {
        btn.addEventListener('click', (e) => {
            e.preventDefault();
            e.stopPropagation();
            const tab = btn.dataset.tab;
            console.log('🔧 [Tabs] Tab clicada:', tab);
            activateTab(tab);
        });
    });
    
    // Ativar tab padrão (Previsão do Tempo)
    console.log('🔧 [Tabs] Ativando tab padrão: previsao-tempo');
    activateTab('previsao-tempo');
    
    // Atualizar label inicial do toggle
    if (window.innerWidth <= 768 && toggle) {
        const activeTab = document.querySelector('.user-tab-btn.active');
        const activeLabel = document.getElementById('user-tabs-active-label');
        if (activeTab && activeLabel) {
            activeLabel.textContent = activeTab.textContent;
        }
    }
    
    console.log('✅ [Tabs] Tabs configuradas');
}

// Setup User Forms
function setupUserForms() {
    // Add Device Form
    const addDeviceForm = document.getElementById('add-device-form');
    if (addDeviceForm) {
        addDeviceForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            const name = document.getElementById('new-device-name').value;
            const comando = document.getElementById('new-device-comando').value;

            try {
                const response = await fetch(`${API_BASE_URL}/api/devices`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'Authorization': `Bearer ${authToken}`
                    },
                    body: JSON.stringify({ name, comando })
                });

                if (!response.ok) {
                    const error = await response.json();
                    throw new Error(error || 'Erro ao criar dispositivo');
                }

                alert('Dispositivo criado com sucesso!');
                document.getElementById('add-device-modal').style.display = 'none';
                addDeviceForm.reset();
                loadUserDevices();
                loadUserStats();
            } catch (error) {
                alert('Erro: ' + error.message);
            }
        });
    }

    // Edit Device Form (User)
    const editUserDeviceForm = document.getElementById('edit-user-device-form');
    if (editUserDeviceForm) {
        editUserDeviceForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            const deviceId = document.getElementById('edit-user-device-id').value;
            const name = document.getElementById('edit-user-device-name').value;
            const comando = document.getElementById('edit-user-device-comando').value;

            try {
                const response = await fetch(`${API_BASE_URL}/api/devices/${deviceId}`, {
                    method: 'PUT',
                    headers: {
                        'Content-Type': 'application/json',
                        'Authorization': `Bearer ${authToken}`
                    },
                    body: JSON.stringify({ newName: name, newComando: comando })
                });

                if (!response.ok) throw new Error('Erro ao atualizar dispositivo');

                alert('Dispositivo atualizado com sucesso!');
                document.getElementById('edit-user-device-modal').style.display = 'none';
                loadUserDevices();
            } catch (error) {
                alert('Erro: ' + error.message);
            }
        });
    }

    // Add Comando Form
    const addComandoForm = document.getElementById('add-comando-form');
    if (addComandoForm) {
        addComandoForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            const comando = document.getElementById('new-comando-comando').value;
            const resposta = document.getElementById('new-comando-resposta').value;
            const estilo = document.getElementById('new-comando-estilo').value;

            try {
                // Tenta primeiro com ComandosSociais, depois com kebab-case
                let response = await fetch(`${API_BASE_URL}/api/ComandosSociais`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'Authorization': `Bearer ${authToken}`
                    },
                    body: JSON.stringify({ comando, resposta, estilo })
                });

                if (response.status === 404) {
                    response = await fetch(`${API_BASE_URL}/api/comandos-sociais`, {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json',
                            'Authorization': `Bearer ${authToken}`
                        },
                        body: JSON.stringify({ comando, resposta, estilo })
                    });
                }

                if (!response.ok) {
                    const error = await response.json();
                    throw new Error(error || 'Erro ao criar comando social');
                }

                alert('Comando social criado com sucesso!');
                document.getElementById('add-comando-modal').style.display = 'none';
                addComandoForm.reset();
                loadUserComandos();
                loadUserStats();
                loadUserInfo(); // Refresh coins
            } catch (error) {
                alert('Erro: ' + error.message);
            }
        });
    }

    // Edit Comando Form (User)
    const editUserComandoForm = document.getElementById('edit-user-comando-form');
    if (editUserComandoForm) {
        editUserComandoForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            const comandoId = document.getElementById('edit-user-comando-id').value;
            const comando = document.getElementById('edit-user-comando-comando').value;
            const resposta = document.getElementById('edit-user-comando-resposta').value;
            const estilo = document.getElementById('edit-user-comando-estilo').value;

            try {
                // Tenta primeiro com ComandosSociais, depois com kebab-case
                let response = await fetch(`${API_BASE_URL}/api/ComandosSociais/${comandoId}`, {
                    method: 'PUT',
                    headers: {
                        'Content-Type': 'application/json',
                        'Authorization': `Bearer ${authToken}`
                    },
                    body: JSON.stringify({ comando, resposta, estilo })
                });

                if (response.status === 404) {
                    response = await fetch(`${API_BASE_URL}/api/comandos-sociais/${comandoId}`, {
                        method: 'PUT',
                        headers: {
                            'Content-Type': 'application/json',
                            'Authorization': `Bearer ${authToken}`
                        },
                        body: JSON.stringify({ comando, resposta, estilo })
                    });
                }

                if (!response.ok) throw new Error('Erro ao atualizar comando social');

                alert('Comando social atualizado com sucesso!');
                document.getElementById('edit-user-comando-modal').style.display = 'none';
                loadUserComandos();
                loadUserInfo(); // Refresh coins
            } catch (error) {
                alert('Erro: ' + error.message);
            }
        });
    }

    // Edit Profile Form
    const editProfileForm = document.getElementById('edit-profile-form');
    if (editProfileForm) {
        editProfileForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            const name = document.getElementById('edit-profile-name').value;
            const email = document.getElementById('edit-profile-email').value;
            const estado = document.getElementById('edit-profile-estado').value;
            const cidade = document.getElementById('edit-profile-cidade').value;
            const bairro = document.getElementById('edit-profile-bairro').value;

            try {
                const response = await fetch(`${API_BASE_URL}/api/users/me`, {
                    method: 'PUT',
                    headers: {
                        'Content-Type': 'application/json',
                        'Authorization': `Bearer ${authToken}`
                    },
                    body: JSON.stringify({ name, email, estado, cidade, bairro })
                });

                if (!response.ok) {
                    const error = await response.json();
                    throw new Error(error.message || 'Erro ao atualizar perfil');
                }

                alert('Perfil atualizado com sucesso!');
                document.getElementById('edit-profile-modal').style.display = 'none';
                loadUserInfo();
            } catch (error) {
                alert('Erro: ' + error.message);
            }
        });
    }

    // Add Funds Form
    const addFundsForm = document.getElementById('add-funds-form');
    if (addFundsForm) {
        addFundsForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            const amount = parseFloat(document.getElementById('funds-amount').value);

            if (amount <= 0) {
                alert('Valor deve ser maior que zero');
                return;
            }

            try {
                const response = await fetch(`${API_BASE_URL}/api/users/add-funds`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'Authorization': `Bearer ${authToken}`
                    },
                    body: JSON.stringify({ amount })
                });

                if (!response.ok) {
                    const error = await response.json();
                    throw new Error(error || 'Erro ao criar sessão de pagamento');
                }

                const data = await response.json();
                window.location.href = data.checkoutUrl;
            } catch (error) {
                alert('Erro: ' + error.message);
            }
        });
    }

    // Change Password Form
    const changePasswordForm = document.getElementById('change-password-form');
    if (changePasswordForm) {
        changePasswordForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            const currentPassword = document.getElementById('current-password').value;
            const newPassword = document.getElementById('new-password').value;
            const confirmPassword = document.getElementById('confirm-password').value;

            if (newPassword !== confirmPassword) {
                alert('As senhas não coincidem');
                return;
            }

            if (newPassword.length < 6) {
                alert('A senha deve ter no mínimo 6 caracteres');
                return;
            }

            try {
                const response = await fetch(`${API_BASE_URL}/api/users/change-password`, {
                    method: 'PUT',
                    headers: {
                        'Content-Type': 'application/json',
                        'Authorization': `Bearer ${authToken}`
                    },
                    body: JSON.stringify({ currentPassword, newPassword })
                });

                if (!response.ok) {
                    const error = await response.json();
                    throw new Error(error || 'Erro ao alterar senha');
                }

                alert('Senha alterada com sucesso!');
                document.getElementById('change-password-modal').style.display = 'none';
                changePasswordForm.reset();
            } catch (error) {
                alert('Erro: ' + error.message);
            }
        });
    }

    // Delete Account Form
    const deleteAccountForm = document.getElementById('delete-account-form');
    if (deleteAccountForm) {
        deleteAccountForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            const password = document.getElementById('delete-password').value;

            if (!confirm('Tem certeza que deseja excluir sua conta? Esta ação é irreversível!')) {
                return;
            }

            try {
                const response = await fetch(`${API_BASE_URL}/api/users/me`, {
                    method: 'DELETE',
                    headers: {
                        'Content-Type': 'application/json',
                        'Authorization': `Bearer ${authToken}`
                    },
                    body: JSON.stringify({ password })
                });

                if (!response.ok) {
                    const error = await response.json();
                    throw new Error(error.message || 'Erro ao excluir conta');
                }

                alert('Conta excluída com sucesso!');
                logout();
            } catch (error) {
                alert('Erro: ' + error.message);
            }
        });
    }
}

// Modal Functions
function openAddDeviceModal() {
    document.getElementById('add-device-modal').style.display = 'block';
}

function openEditProfileModal() {
    // Load current user data
    fetch(`${API_BASE_URL}/api/users/me`, {
        headers: { 'Authorization': `Bearer ${authToken}` }
    })
    .then(r => r.json())
    .then(user => {
        document.getElementById('edit-profile-name').value = user.name || '';
        document.getElementById('edit-profile-email').value = user.email || '';
        document.getElementById('edit-profile-estado').value = user.estado || '';
        document.getElementById('edit-profile-cidade').value = user.cidade || '';
        document.getElementById('edit-profile-bairro').value = user.bairro || '';
        document.getElementById('edit-profile-modal').style.display = 'block';
    })
    .catch(err => {
        alert('Erro ao carregar dados do perfil');
        console.error(err);
    });
}

function openAddFundsModal() {
    document.getElementById('add-funds-modal').style.display = 'block';
}

function setFundsAmount(amount) {
    document.getElementById('funds-amount').value = amount;
}

function openPlanoModal() {
    document.getElementById('plano-modal').style.display = 'block';
}

async function contratarPlano(nivel) {
    try {
        const response = await fetch(`${API_BASE_URL}/api/assinaturas/checkout`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${authToken}`
            },
            body: JSON.stringify({ nivel })
        });

        if (!response.ok) {
            let errorMessage = 'Erro ao criar checkout';
            try {
                const errorData = await response.json();
                errorMessage = errorData.error || errorData.message || JSON.stringify(errorData);
            } catch (e) {
                // Se não conseguir parsear JSON, tenta ler como texto
                const errorText = await response.text();
                errorMessage = errorText || 'Erro desconhecido ao criar checkout';
            }
            alert('Erro: ' + errorMessage);
            console.error('Erro ao criar checkout:', errorMessage);
            return;
        }

        const data = await response.json();
        if (data.checkoutUrl) {
        window.location.href = data.checkoutUrl;
        } else {
            alert('Erro: URL de checkout não recebida');
        }
    } catch (error) {
        const errorMsg = error.message || 'Erro inesperado ao criar checkout';
        alert('Erro: ' + errorMsg);
        console.error('Erro ao criar checkout:', error);
    }
}

function openChangePasswordModal() {
    document.getElementById('change-password-modal').style.display = 'block';
}

function openDeleteAccountModal() {
    document.getElementById('delete-account-modal').style.display = 'block';
}

async function editUserDevice(deviceId) {
    try {
        const response = await fetch(`${API_BASE_URL}/api/devices/${deviceId}`, {
            headers: { 'Authorization': `Bearer ${authToken}` }
        });

        if (!response.ok) throw new Error('Erro ao carregar dispositivo');

        const device = await response.json();
        document.getElementById('edit-user-device-id').value = device.id;
        document.getElementById('edit-user-device-name').value = device.name || '';
        document.getElementById('edit-user-device-comando').value = device.comando || '';
        document.getElementById('edit-user-device-modal').style.display = 'block';
    } catch (error) {
        alert('Erro: ' + error.message);
    }
}

async function deleteUserDevice(deviceId) {
    if (!confirm('Tem certeza que deseja excluir este dispositivo?')) return;

    try {
        const response = await fetch(`${API_BASE_URL}/api/devices/${deviceId}`, {
            method: 'DELETE',
            headers: { 'Authorization': `Bearer ${authToken}` }
        });

        if (!response.ok) throw new Error('Erro ao excluir dispositivo');

        alert('Dispositivo excluído com sucesso!');
        loadUserDevices();
        loadUserStats();
    } catch (error) {
        alert('Erro: ' + error.message);
    }
}

async function acionarDevice(deviceId, comando) {
    try {
        const response = await fetch(`${API_BASE_URL}/api/commands/publish`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${authToken}`
            },
            body: JSON.stringify({
                deviceId: deviceId,
                customCommand: comando
            })
        });

        if (!response.ok) {
            const error = await response.json();
            throw new Error(error || 'Erro ao acionar dispositivo');
        }

        alert(`Comando "${comando}" enviado com sucesso!`);
    } catch (error) {
        alert('Erro: ' + error.message);
    }
}

async function editUserComando(comandoId) {
    try {
        // Tenta primeiro com ComandosSociais, depois com kebab-case
        let response = await fetch(`${API_BASE_URL}/api/ComandosSociais`, {
            headers: { 'Authorization': `Bearer ${authToken}` }
        });

        if (response.status === 404) {
            response = await fetch(`${API_BASE_URL}/api/comandos-sociais`, {
                headers: { 'Authorization': `Bearer ${authToken}` }
            });
        }

        if (!response.ok) throw new Error('Erro ao carregar comandos');

        const comandos = await response.json();
        const comando = comandos.find(c => c.id === comandoId);

        if (!comando) throw new Error('Comando não encontrado');

        document.getElementById('edit-user-comando-id').value = comando.id;
        document.getElementById('edit-user-comando-comando').value = comando.comando || '';
        document.getElementById('edit-user-comando-resposta').value = comando.resposta || '';
        document.getElementById('edit-user-comando-estilo').value = comando.estilo || '';
        document.getElementById('edit-user-comando-modal').style.display = 'block';
    } catch (error) {
        alert('Erro: ' + error.message);
    }
}

async function deleteUserComando(comandoId) {
    if (!confirm('Tem certeza que deseja excluir este comando social?')) return;

    try {
        // Tenta primeiro com ComandosSociais, depois com kebab-case
        let response = await fetch(`${API_BASE_URL}/api/ComandosSociais/${comandoId}`, {
            method: 'DELETE',
            headers: { 'Authorization': `Bearer ${authToken}` }
        });

        if (response.status === 404) {
            response = await fetch(`${API_BASE_URL}/api/comandos-sociais/${comandoId}`, {
                method: 'DELETE',
                headers: { 'Authorization': `Bearer ${authToken}` }
            });
        }

        if (!response.ok) throw new Error('Erro ao excluir comando social');

        alert('Comando social excluído com sucesso!');
        loadUserComandos();
        loadUserStats();
    } catch (error) {
        alert('Erro: ' + error.message);
    }
}

function openAddComandoModal() {
    document.getElementById('add-comando-modal').style.display = 'block';
}

function copyApiKey() {
    const apiKey = document.getElementById('user-api-key').textContent;
    navigator.clipboard.writeText(apiKey).then(() => {
        alert('API Key copiada para a área de transferência!');
    }).catch(err => {
        alert('Erro ao copiar API Key');
        console.error(err);
    });
}

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// ========== DISPOSITIVOS ESP ==========
let dispositivoEspHubConnection = null;

// Conectar ao WebSocket DispositivoESP
function connectDispositivoEspHub() {
    if (!authToken) return;

    const hubUrl = `${API_BASE_URL}/hubs/dispositivo-esp`;
    
    dispositivoEspHubConnection = new signalR.HubConnectionBuilder()
        .withUrl(hubUrl, {
            accessTokenFactory: () => authToken
        })
        .withAutomaticReconnect()
        .build();

    dispositivoEspHubConnection.on("Connected", (data) => {
        console.log("Conectado ao DispositivoESP Hub:", data);
    });

    dispositivoEspHubConnection.on("RespostaDispositivo", (data) => {
        console.log("Resposta recebida:", data);
        // Não exibir alert para nenhuma resposta via websocket
        // As mensagens "toApp:" são destinadas ao software/app, mas não bloqueamos aqui
        // apenas não exibimos alert na interface web
    });

    dispositivoEspHubConnection.on("StatusDispositivoAtualizado", (data) => {
        console.log("Status atualizado:", data);
        // Atualiza a lista de dispositivos
        if (document.getElementById('dispositivos-esp-list')) {
            loadUserDispositivosEsp();
        }
        // Admin não tem mais lista de dispositivos ESP
    });

    dispositivoEspHubConnection.on("LogErro", (data) => {
        console.error("Log de erro:", data);
    });

    dispositivoEspHubConnection.on("DadosUso", (data) => {
        console.log("Dados de uso:", data);
    });

    // Handler para ToAppResposta - mensagens destinadas ao software/app, não para a web
    dispositivoEspHubConnection.on("ToAppResposta", (data) => {
        console.log("ToAppResposta recebida (ignorada - destinada ao software/app):", data);
        // Não processar - essas mensagens são para o software Windows ou app Kotlin
    });

    dispositivoEspHubConnection.start()
        .then(() => {
            console.log("Conectado ao DispositivoESP Hub");
            dispositivoEspHubConnection.invoke("IdentificarCliente", "web", currentUser?.id);
        })
        .catch(err => console.error("Erro ao conectar ao DispositivoESP Hub:", err));
}

// Load Online Users
async function loadOnlineUsers() {
    try {
        const response = await fetchWithAuth(`${API_BASE_URL}/api/users/online`);

        if (!response.ok) throw new Error('Erro ao carregar usuários online');

        const users = await response.json();
        const tbody = document.getElementById('online-users-table-body');
        
        if (users.length === 0) {
            tbody.innerHTML = '<tr><td colspan="6" class="loading">Nenhum usuário online</td></tr>';
            return;
        }

        tbody.innerHTML = users.map(user => `
            <tr>
                <td>${escapeHtml(user.name)}</td>
                <td>${escapeHtml(user.email)}</td>
                <td><span class="role-badge">${escapeHtml(user.role)}</span></td>
                <td>${user.starkCoins.toFixed(2)}</td>
                <td><span class="status-badge active">${escapeHtml(user.origem)}</span></td>
                <td>
                    <div class="action-buttons">
                        <button class="action-btn view" onclick="viewUserDetails('${user.id}')">Ver Detalhes</button>
                        <button class="action-btn edit" onclick="editUser('${user.id}')">Editar</button>
                        <button class="action-btn" onclick="disconnectUser('${user.id}')" style="background: rgba(239, 68, 68, 0.2); color: var(--error-color);" disabled>Desconectar</button>
                        <button class="action-btn" onclick="sendMessageToUser('${user.id}')" disabled>Enviar Mensagem</button>
                        <button class="action-btn" onclick="viewErrorLogsApp('${user.id}')" disabled>LogsErrorApp</button>
                        <button class="action-btn" onclick="viewErrorLogsSoft('${user.id}')" disabled>LogsErrorSoft</button>
                    </div>
                </td>
            </tr>
        `).join('');
    } catch (error) {
        console.error('Erro ao carregar usuários online:', error);
        document.getElementById('online-users-table-body').innerHTML = 
            '<tr><td colspan="6" class="loading">Erro ao carregar usuários online</td></tr>';
    }
}

// Refresh Online Users
function refreshOnlineUsers() {
    loadOnlineUsers();
}

// Load Users with Active Plans
async function loadUsersWithPlans() {
    try {
        const response = await fetchWithAuth(`${API_BASE_URL}/api/admin/users-with-plans`);

        if (!response.ok) throw new Error('Erro ao carregar usuários com planos ativos');

        const users = await response.json();
        const tbody = document.getElementById('users-with-plans-table-body');
        
        if (users.length === 0) {
            tbody.innerHTML = '<tr><td colspan="9" class="loading">Nenhum usuário com plano ativo</td></tr>';
            return;
        }

        tbody.innerHTML = users.map(user => {
            const expiraEm = user.expiraEm 
                ? new Date(user.expiraEm).toLocaleDateString('pt-BR', { 
                    day: '2-digit', 
                    month: '2-digit', 
                    year: 'numeric',
                    hour: '2-digit',
                    minute: '2-digit'
                })
                : 'Não expira';
            
            return `
                <tr>
                    <td>${escapeHtml(user.name)}</td>
                    <td>${escapeHtml(user.email)}</td>
                    <td><span class="role-badge">${escapeHtml(user.role)}</span></td>
                    <td>${user.starkCoins.toFixed(2)}</td>
                    <td><span class="status-badge active">${escapeHtml(user.plano)}</span></td>
                    <td>R$ ${user.valor.toFixed(2)}</td>
                    <td><span class="status-badge active">${escapeHtml(user.status)}</span></td>
                    <td>${expiraEm}</td>
                    <td>
                        <div class="action-buttons">
                            <button class="action-btn view" onclick="viewUserDetails('${user.id}')">Ver Detalhes</button>
                            <button class="action-btn edit" onclick="editUser('${user.id}')">Editar</button>
                        </div>
                    </td>
                </tr>
            `;
        }).join('');
    } catch (error) {
        console.error('Erro ao carregar usuários com planos ativos:', error);
        document.getElementById('users-with-plans-table-body').innerHTML = 
            '<tr><td colspan="9" class="loading">Erro ao carregar usuários com planos ativos</td></tr>';
    }
}

// Refresh Users with Plans
function refreshUsersWithPlans() {
    loadUsersWithPlans();
}

// Load Starkcoins Vendas
async function loadStarkcoinsVendas() {
    try {
        const response = await fetchWithAuth(`${API_BASE_URL}/api/admin/starkcoins-vendas`);

        if (!response.ok) throw new Error('Erro ao carregar vendas de StarkCoins');

        const data = await response.json();
        const vendas = data.vendas || [];
        const total = data.total || 0;
        const tbody = document.getElementById('starkcoins-vendas-table-body');
        const totalElement = document.getElementById('starkcoins-vendas-total');
        
        // Atualizar total
        if (totalElement) {
            totalElement.textContent = `Total: R$ ${total.toFixed(2).replace('.', ',')}`;
        }
        
        if (vendas.length === 0) {
            tbody.innerHTML = '<tr><td colspan="7" class="loading">Nenhuma venda encontrada</td></tr>';
            return;
        }

        tbody.innerHTML = vendas.map(venda => {
            const dataCompleta = new Date(venda.data);
            const data = dataCompleta.toLocaleDateString('pt-BR', { 
                day: '2-digit', 
                month: '2-digit', 
                year: 'numeric'
            });
            const horario = dataCompleta.toLocaleTimeString('pt-BR', { 
                hour: '2-digit', 
                minute: '2-digit'
            });
            
            // Formatar status para exibir "Concluído" em vez de "Pago"
            const statusFormatado = venda.status === 'Pago' || venda.status === 'pago' ? 'Concluído' : venda.status;
            
            return `
                <tr>
                    <td>${data}</td>
                    <td>${horario}</td>
                    <td>R$ ${venda.valor.toFixed(2).replace('.', ',')}</td>
                    <td>${escapeHtml(venda.usuarioNome)}</td>
                    <td>${escapeHtml(venda.usuarioEmail)}</td>
                    <td><span class="status-badge active">${escapeHtml(statusFormatado)}</span></td>
                    <td>
                        <div class="action-buttons">
                            <button class="action-btn" onclick="deleteStarkcoinsVenda('${venda.id}')" style="background: rgba(239, 68, 68, 0.2); color: var(--error-color);">Apagar registro</button>
                        </div>
                    </td>
                </tr>
            `;
        }).join('');
    } catch (error) {
        console.error('Erro ao carregar vendas de StarkCoins:', error);
        document.getElementById('starkcoins-vendas-table-body').innerHTML = 
            '<tr><td colspan="7" class="loading">Erro ao carregar vendas de StarkCoins</td></tr>';
    }
}

// Refresh Starkcoins Vendas
function refreshStarkcoinsVendas() {
    loadStarkcoinsVendas();
}

// Delete Starkcoins Venda
async function deleteStarkcoinsVenda(vendaId) {
    if (!confirm('Tem certeza que deseja apagar este registro de venda?')) {
        return;
    }

    try {
        const response = await fetchWithAuth(`${API_BASE_URL}/api/admin/starkcoins-vendas/${vendaId}`, {
            method: 'DELETE'
        });

        if (!response.ok) {
            const error = await response.json();
            throw new Error(error.message || 'Erro ao apagar registro');
        }

        // Recarregar a lista após deletar
        loadStarkcoinsVendas();
    } catch (error) {
        console.error('Erro ao apagar registro de venda:', error);
        alert('Erro ao apagar registro: ' + error.message);
    }
}

// Load Pagamentos com Falhas
async function loadPagamentosFalhas() {
    try {
        const response = await fetchWithAuth(`${API_BASE_URL}/api/admin/pagamentos-falhas`);

        if (!response.ok) throw new Error('Erro ao carregar pagamentos com falhas');

        const pagamentos = await response.json();
        const tbody = document.getElementById('pagamentos-falhas-table-body');
        
        if (pagamentos.length === 0) {
            tbody.innerHTML = '<tr><td colspan="9" class="loading">Nenhum pagamento com falha encontrado</td></tr>';
            return;
        }

        tbody.innerHTML = pagamentos.map(pagamento => {
            const dataCompleta = new Date(pagamento.data);
            const data = dataCompleta.toLocaleDateString('pt-BR', { 
                day: '2-digit', 
                month: '2-digit', 
                year: 'numeric'
            });
            const horario = dataCompleta.toLocaleTimeString('pt-BR', { 
                hour: '2-digit', 
                minute: '2-digit'
            });
            
            return `
                <tr>
                    <td>${data}</td>
                    <td>${horario}</td>
                    <td>R$ ${pagamento.valor.toFixed(2).replace('.', ',')}</td>
                    <td>${escapeHtml(pagamento.usuarioNome)}</td>
                    <td>${escapeHtml(pagamento.usuarioEmail)}</td>
                    <td><span class="status-badge" style="background: rgba(239, 68, 68, 0.2); color: var(--error-color);">${escapeHtml(pagamento.status)}</span></td>
                    <td><code style="font-size: 0.9rem; color: var(--text-secondary);">${escapeHtml(pagamento.codigoErro)}</code></td>
                    <td>${escapeHtml(pagamento.detalheErro)}</td>
                    <td>
                        <div class="action-buttons">
                            <button class="action-btn" onclick="deletePagamentoFalha('${pagamento.id}')" style="background: rgba(239, 68, 68, 0.2); color: var(--error-color);">Apagar registro</button>
                        </div>
                    </td>
                </tr>
            `;
        }).join('');
    } catch (error) {
        console.error('Erro ao carregar pagamentos com falhas:', error);
        document.getElementById('pagamentos-falhas-table-body').innerHTML = 
            '<tr><td colspan="9" class="loading">Erro ao carregar pagamentos com falhas</td></tr>';
    }
}

// Refresh Pagamentos com Falhas
function refreshPagamentosFalhas() {
    loadPagamentosFalhas();
}

// Delete Pagamento Falha
async function deletePagamentoFalha(pagamentoId) {
    if (!confirm('Tem certeza que deseja apagar este registro de pagamento com falha?')) {
        return;
    }

    try {
        const response = await fetchWithAuth(`${API_BASE_URL}/api/admin/pagamentos-falhas/${pagamentoId}`, {
            method: 'DELETE'
        });

        if (!response.ok) {
            const error = await response.json();
            throw new Error(error.message || 'Erro ao apagar registro');
        }

        // Recarregar a lista após deletar
        loadPagamentosFalhas();
    } catch (error) {
        console.error('Erro ao apagar registro de pagamento:', error);
        alert('Erro ao apagar registro: ' + error.message);
    }
}

// Load Error Logs Users
async function loadErrorLogsUsers() {
    try {
        const response = await fetchWithAuth(`${API_BASE_URL}/api/admin/error-logs/users`);

        if (!response.ok) throw new Error('Erro ao carregar usuários com logs de erro');

        const users = await response.json();
        const tbody = document.getElementById('error-logs-table-body');
        
        if (users.length === 0) {
            tbody.innerHTML = '<tr><td colspan="4" class="loading">Nenhum usuário com logs de erro encontrado</td></tr>';
            return;
        }

        tbody.innerHTML = users.map(user => {
            return `
                <tr>
                    <td>${escapeHtml(user.userId)}</td>
                    <td>${escapeHtml(user.userName)}</td>
                    <td>${escapeHtml(user.userEmail)}</td>
                    <td>
                        <div class="action-buttons">
                            <button class="action-btn view" onclick="viewErrorLogsSoft('${user.userId}')">Ver logsErrorSoft</button>
                            <button class="action-btn view" onclick="viewErrorLogsApp('${user.userId}')">Ver logsErrorApp</button>
                        </div>
                    </td>
                </tr>
            `;
        }).join('');
    } catch (error) {
        console.error('Erro ao carregar usuários com logs de erro:', error);
        document.getElementById('error-logs-table-body').innerHTML = 
            '<tr><td colspan="4" class="loading">Erro ao carregar usuários com logs de erro</td></tr>';
    }
}

// Refresh Error Logs
function refreshErrorLogs() {
    loadErrorLogsUsers();
}

// View Error Logs Soft
async function viewErrorLogsSoft(userId) {
    try {
        const response = await fetchWithAuth(`${API_BASE_URL}/api/admin/error-logs/soft/${userId}`);

        if (!response.ok) throw new Error('Erro ao carregar logs de erro');

        const logs = await response.json();
        
        // Criar modal para exibir logs
        const modal = document.createElement('div');
        modal.className = 'modal';
        modal.style.display = 'block';
        modal.innerHTML = `
            <div class="modal-content" style="max-width: 90%; max-height: 90vh; overflow-y: auto;">
                <div class="modal-header">
                    <h2>Logs de Erro - Soft (${logs.length} registros)</h2>
                    <span class="close" onclick="this.closest('.modal').remove()">&times;</span>
                </div>
                <div class="modal-body">
                    <table class="users-table" style="width: 100%;">
                        <thead>
                            <tr>
                                <th>Data</th>
                                <th>Hora</th>
                                <th>Código Erro</th>
                                <th>Ação</th>
                                <th>Último Comando</th>
                                <th>Última Resposta</th>
                                <th>Dispositivo</th>
                                <th>Erro Completo</th>
                                <th>Ações</th>
                            </tr>
                        </thead>
                        <tbody>
                            ${logs.map(log => `
                                <tr>
                                    <td>${escapeHtml(log.dataErro || 'N/A')}</td>
                                    <td>${escapeHtml(log.horaErro || 'N/A')}</td>
                                    <td><code>${escapeHtml(log.codigoDeErro || 'N/A')}</code></td>
                                    <td>${escapeHtml(log.acaoErro || 'N/A')}</td>
                                    <td>${escapeHtml(log.ultimoComando || 'N/A')}</td>
                                    <td>${escapeHtml(log.ultimaResposta || 'N/A')}</td>
                                    <td>${escapeHtml(log.ultimoDispositivoAcionado || 'N/A')}</td>
                                    <td><pre style="max-width: 300px; white-space: pre-wrap; word-wrap: break-word;">${escapeHtml(log.erroCompleto || 'N/A')}</pre></td>
                                    <td>
                                        <button class="action-btn" onclick="deleteErrorLogSoft(${log.id}, '${userId}')" style="background: rgba(239, 68, 68, 0.2); color: var(--error-color);">Apagar</button>
                                    </td>
                                </tr>
                            `).join('')}
                        </tbody>
                    </table>
                </div>
            </div>
        `;
        document.body.appendChild(modal);
        
        // Fechar modal ao clicar fora
        modal.addEventListener('click', function(e) {
            if (e.target === modal) {
                modal.remove();
            }
        });
    } catch (error) {
        console.error('Erro ao carregar logs de erro:', error);
        alert('Erro ao carregar logs de erro: ' + error.message);
    }
}

// Delete Error Log Soft
async function deleteErrorLogSoft(logId, userId) {
    if (!confirm('Tem certeza que deseja apagar este log de erro?')) {
        return;
    }

    try {
        const response = await fetchWithAuth(`${API_BASE_URL}/api/admin/error-logs/soft/${logId}`, {
            method: 'DELETE'
        });

        if (!response.ok) {
            const error = await response.json();
            throw new Error(error.message || 'Erro ao apagar log');
        }

        // Recarregar a lista de logs
        viewErrorLogsSoft(userId);
    } catch (error) {
        console.error('Erro ao apagar log de erro:', error);
        alert('Erro ao apagar log: ' + error.message);
    }
}

// Consultar Codigo de Erro Soft
async function consultarCodigoErroSoft() {
    const codigo = document.getElementById('codigo-erro-soft').value.trim();
    const resultadoDiv = document.getElementById('resultado-codigo-soft');
    const conteudoDiv = document.getElementById('conteudo-resultado-soft');

    if (!codigo) {
        alert('Por favor, insira um código de erro.');
        return;
    }

    try {
        const response = await fetchWithAuth(`${API_BASE_URL}/api/logs/error-code-soft/${encodeURIComponent(codigo)}`);

        if (!response.ok) throw new Error('Erro ao consultar código de erro');

        const data = await response.json();
        
        conteudoDiv.innerHTML = `
            <div style="display: grid; gap: 1rem;">
                <div>
                    <strong style="color: var(--primary-color);">Código de Erro:</strong>
                    <div style="margin-top: 0.5rem; padding: 0.5rem; background: var(--dark-surface); border-radius: 6px;">
                        <code style="color: var(--light-text);">${escapeHtml(data.codigoDeErro)}</code>
                    </div>
                </div>
                <div>
                    <strong style="color: var(--primary-color);">Descrição:</strong>
                    <div style="margin-top: 0.5rem; padding: 0.5rem; background: var(--dark-surface); border-radius: 6px;">
                        ${escapeHtml(data.descricao)}
                    </div>
                </div>
                <div>
                    <strong style="color: var(--primary-color);">Contexto:</strong>
                    <div style="margin-top: 0.5rem; padding: 0.5rem; background: var(--dark-surface); border-radius: 6px;">
                        ${escapeHtml(data.contexto)}
                    </div>
                </div>
                <div>
                    <strong style="color: var(--primary-color);">Campos Relevantes:</strong>
                    <div style="margin-top: 0.5rem; padding: 0.5rem; background: var(--dark-surface); border-radius: 6px;">
                        ${escapeHtml(data.camposRelevantes)}
                    </div>
                </div>
            </div>
        `;
        
        resultadoDiv.style.display = 'block';
    } catch (error) {
        console.error('Erro ao consultar código de erro:', error);
        conteudoDiv.innerHTML = `<div style="color: var(--error-color);">Erro ao consultar código de erro: ${escapeHtml(error.message)}</div>`;
        resultadoDiv.style.display = 'block';
    }
}

// Consultar Codigo de Erro App
async function consultarCodigoErroApp() {
    const codigo = document.getElementById('codigo-erro-app').value.trim();
    const resultadoDiv = document.getElementById('resultado-codigo-app');
    const conteudoDiv = document.getElementById('conteudo-resultado-app');

    if (!codigo) {
        alert('Por favor, insira um código de erro.');
        return;
    }

    try {
        const response = await fetchWithAuth(`${API_BASE_URL}/api/logs/error-code-app/${encodeURIComponent(codigo)}`);

        if (!response.ok) throw new Error('Erro ao consultar código de erro');

        const data = await response.json();
        
        conteudoDiv.innerHTML = `
            <div style="display: grid; gap: 1rem;">
                <div>
                    <strong style="color: var(--primary-color);">Código de Erro:</strong>
                    <div style="margin-top: 0.5rem; padding: 0.5rem; background: var(--dark-surface); border-radius: 6px;">
                        <code style="color: var(--light-text);">${escapeHtml(data.codigoDeErro)}</code>
                    </div>
                </div>
                <div>
                    <strong style="color: var(--primary-color);">Descrição:</strong>
                    <div style="margin-top: 0.5rem; padding: 0.5rem; background: var(--dark-surface); border-radius: 6px;">
                        ${escapeHtml(data.descricao)}
                    </div>
                </div>
                <div>
                    <strong style="color: var(--primary-color);">Contexto:</strong>
                    <div style="margin-top: 0.5rem; padding: 0.5rem; background: var(--dark-surface); border-radius: 6px;">
                        ${escapeHtml(data.contexto)}
                    </div>
                </div>
                <div>
                    <strong style="color: var(--primary-color);">Campos Relevantes:</strong>
                    <div style="margin-top: 0.5rem; padding: 0.5rem; background: var(--dark-surface); border-radius: 6px;">
                        ${escapeHtml(data.camposRelevantes)}
                    </div>
                </div>
            </div>
        `;
        
        resultadoDiv.style.display = 'block';
    } catch (error) {
        console.error('Erro ao consultar código de erro:', error);
        conteudoDiv.innerHTML = `<div style="color: var(--error-color);">Erro ao consultar código de erro: ${escapeHtml(error.message)}</div>`;
        resultadoDiv.style.display = 'block';
    }
}

// Buscar Soluções para Código de Erro Soft
async function buscarSolucoesSoft() {
    const codigo = document.getElementById('codigo-solucao-soft').value.trim();
    const resultadoDiv = document.getElementById('solucoes-soft-result');
    const listaDiv = document.getElementById('solucoes-soft-list');

    if (!codigo) {
        alert('Por favor, insira um código de erro.');
        return;
    }

    try {
        const response = await fetchWithAuth(`${API_BASE_URL}/api/logs/error-solutions-soft/${encodeURIComponent(codigo)}`);

        if (!response.ok) {
            throw new Error('Erro ao buscar soluções');
        }

        const data = await response.json();
        
        if (data.solucoes && data.solucoes.length > 0) {
            listaDiv.innerHTML = data.solucoes.map((solucao, index) => `
                <li style="margin-bottom: 1rem; padding: 1rem; background: var(--dark-bg); border-radius: 8px; border-left: 4px solid var(--primary-color);">
                    <div style="display: flex; align-items: flex-start; gap: 0.75rem;">
                        <span style="color: var(--primary-color); font-weight: bold; min-width: 2rem;">${index + 1}.</span>
                        <span style="color: var(--light-text); line-height: 1.6;">${escapeHtml(solucao)}</span>
                    </div>
                </li>
            `).join('');
            resultadoDiv.style.display = 'block';
        } else {
            listaDiv.innerHTML = '<li style="color: var(--text-secondary); padding: 1rem;">Nenhuma solução encontrada para este código de erro.</li>';
            resultadoDiv.style.display = 'block';
        }
    } catch (error) {
        console.error('Erro ao buscar soluções:', error);
        alert('Erro ao buscar soluções. Por favor, tente novamente.');
    }
}

// Buscar Soluções para Código de Erro App
async function buscarSolucoesApp() {
    const codigo = document.getElementById('codigo-solucao-app').value.trim();
    const resultadoDiv = document.getElementById('solucoes-app-result');
    const listaDiv = document.getElementById('solucoes-app-list');

    if (!codigo) {
        alert('Por favor, insira um código de erro.');
        return;
    }

    try {
        const response = await fetchWithAuth(`${API_BASE_URL}/api/logs/error-solutions-app/${encodeURIComponent(codigo)}`);

        if (!response.ok) {
            throw new Error('Erro ao buscar soluções');
        }

        const data = await response.json();
        
        if (data.solucoes && data.solucoes.length > 0) {
            listaDiv.innerHTML = data.solucoes.map((solucao, index) => `
                <li style="margin-bottom: 1rem; padding: 1rem; background: var(--dark-bg); border-radius: 8px; border-left: 4px solid var(--primary-color);">
                    <div style="display: flex; align-items: flex-start; gap: 0.75rem;">
                        <span style="color: var(--primary-color); font-weight: bold; min-width: 2rem;">${index + 1}.</span>
                        <span style="color: var(--light-text); line-height: 1.6;">${escapeHtml(solucao)}</span>
                    </div>
                </li>
            `).join('');
            resultadoDiv.style.display = 'block';
        } else {
            listaDiv.innerHTML = '<li style="color: var(--text-secondary); padding: 1rem;">Nenhuma solução encontrada para este código de erro.</li>';
            resultadoDiv.style.display = 'block';
        }
    } catch (error) {
        console.error('Erro ao buscar soluções:', error);
        alert('Erro ao buscar soluções. Por favor, tente novamente.');
    }
}

// Placeholder functions for future implementation
function disconnectUser(userId) {
    // Implementar depois
    alert('Funcionalidade será implementada em breve');
}

function sendMessageToUser(userId) {
    // Implementar depois
    alert('Funcionalidade será implementada em breve');
}

// Load User DispositivosESP
async function loadUserDispositivosEsp() {
    try {
        const response = await fetchWithAuth(`${API_BASE_URL}/api/DispositivosEsp`);

        if (!response.ok) throw new Error('Erro ao carregar dispositivos ESP');

        const dispositivos = await response.json();
        const listEl = document.getElementById('dispositivos-esp-list');

        if (dispositivos.length === 0) {
            listEl.innerHTML = '<div class="empty-state"><div class="empty-state-icon">🔌</div><p>Nenhum dispositivo ESP cadastrado</p></div>';
            return;
        }

        listEl.innerHTML = dispositivos.map(d => `
            <div class="item-card">
                <div class="item-card-header">
                    <div class="item-card-title">${escapeHtml(d.nome || 'Sem nome')}</div>
                    <div class="item-card-actions">
                        <button class="btn btn-secondary" onclick="editDispositivoEsp('${d.id}')">Editar</button>
                        <button class="btn btn-secondary" onclick="deleteDispositivoEsp('${d.id}')" style="background: rgba(239, 68, 68, 0.2); color: var(--error-color);">Excluir</button>
                    </div>
                </div>
                <div class="item-card-body">
                    <p><strong>IP:</strong> ${escapeHtml(d.ip || 'N/A')}</p>
                    <p><strong>Porta:</strong> ${d.porta || 'N/A'}</p>
                    <p><strong>Comando:</strong> ${escapeHtml(d.comando || 'N/A')}</p>
                    <p><strong>Comando para ESP:</strong> ${escapeHtml(d.comandToEsp || 'N/A')}</p>
                    <p><strong>Status:</strong> <span class="status-badge ${d.status === 'Conectado' ? 'active' : 'inactive'}">${escapeHtml(d.status || 'Desconectado')}</span></p>
                    <p><strong>Estado:</strong> ${d.ligadoDesligado ? 'Ligado' : 'Desligado'}</p>
                </div>
                <div class="device-actions">
                    <button class="btn btn-secondary" onclick="pingDispositivoEsp('${d.id}')">Ping</button>
                    <button class="btn btn-starkswit" onclick="enviarComandoEsp('${d.id}', '${escapeHtml(d.comando || '')}')">Enviar Comando</button>
                </div>
            </div>
        `).join('');
    } catch (error) {
        console.error('Erro ao carregar dispositivos ESP:', error);
        document.getElementById('dispositivos-esp-list').innerHTML = '<div class="error-message show">Erro ao carregar dispositivos ESP</div>';
    }
}

// Modal Functions
function openAddDispositivoEspModal() {
    document.getElementById('add-dispositivo-esp-modal').style.display = 'block';
}

async function editDispositivoEsp(id) {
    try {
        const response = await fetch(`${API_BASE_URL}/api/DispositivosEsp/${id}`, {
            headers: { 'Authorization': `Bearer ${authToken}` }
        });

        if (!response.ok) throw new Error('Erro ao carregar dispositivo ESP');

        const dispositivo = await response.json();
        document.getElementById('edit-esp-id').value = dispositivo.id;
        document.getElementById('edit-esp-nome').value = dispositivo.nome || '';
        document.getElementById('edit-esp-ip').value = dispositivo.ip || '';
        document.getElementById('edit-esp-porta').value = dispositivo.porta || '';
        document.getElementById('edit-esp-comando').value = dispositivo.comando || '';
        document.getElementById('edit-esp-comandToEsp').value = dispositivo.comandToEsp || '';
        document.getElementById('edit-esp-status').value = dispositivo.status || 'Desconectado';
        document.getElementById('edit-esp-ligado').checked = dispositivo.ligadoDesligado || false;
        document.getElementById('edit-dispositivo-esp-modal').style.display = 'block';
    } catch (error) {
        alert('Erro: ' + error.message);
    }
}

async function deleteDispositivoEsp(id) {
    if (!confirm('Tem certeza que deseja excluir este dispositivo ESP?')) return;

    try {
        const response = await fetch(`${API_BASE_URL}/api/DispositivosEsp/${id}`, {
            method: 'DELETE',
            headers: { 'Authorization': `Bearer ${authToken}` }
        });

        if (!response.ok) throw new Error('Erro ao excluir dispositivo ESP');

        alert('Dispositivo ESP excluído com sucesso!');
        loadUserDispositivosEsp();
    } catch (error) {
        alert('Erro: ' + error.message);
    }
}

async function pingDispositivoEsp(id) {
    try {
        const response = await fetch(`${API_BASE_URL}/api/DispositivosEsp/${id}/ping`, {
            method: 'POST',
            headers: { 'Authorization': `Bearer ${authToken}` }
        });

        if (!response.ok) throw new Error('Erro ao fazer ping');

        const data = await response.json();
        alert(`Status: ${data.status}`);
        loadUserDispositivosEsp();
    } catch (error) {
        alert('Erro: ' + error.message);
    }
}

function enviarComandoEsp(id, comandoPadrao) {
    document.getElementById('comando-esp-id').value = id;
    document.getElementById('comando-esp-texto').value = comandoPadrao || '';
    document.getElementById('enviar-comando-esp-modal').style.display = 'block';
}

// Setup DispositivosESP Forms
function setupDispositivosEspForms() {
    // Add Form
    const addForm = document.getElementById('add-dispositivo-esp-form');
    if (addForm) {
        addForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            const nome = document.getElementById('new-esp-nome').value;
            const ip = document.getElementById('new-esp-ip').value;
            const porta = parseInt(document.getElementById('new-esp-porta').value);
            const comando = document.getElementById('new-esp-comando').value;
            const comandToEsp = document.getElementById('new-esp-comandToEsp').value;

            try {
                const response = await fetch(`${API_BASE_URL}/api/DispositivosEsp`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'Authorization': `Bearer ${authToken}`
                    },
                    body: JSON.stringify({ nome, ip, porta, comando, comandToEsp })
                });

                if (!response.ok) {
                    const error = await response.json();
                    throw new Error(error || 'Erro ao criar dispositivo ESP');
                }

                alert('Dispositivo ESP criado com sucesso!');
                document.getElementById('add-dispositivo-esp-modal').style.display = 'none';
                addForm.reset();
                loadUserDispositivosEsp();
            } catch (error) {
                alert('Erro: ' + error.message);
            }
        });
    }

    // Edit Form
    const editForm = document.getElementById('edit-dispositivo-esp-form');
    if (editForm) {
        editForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            const id = document.getElementById('edit-esp-id').value;
            const nome = document.getElementById('edit-esp-nome').value;
            const ip = document.getElementById('edit-esp-ip').value;
            const porta = parseInt(document.getElementById('edit-esp-porta').value);
            const comando = document.getElementById('edit-esp-comando').value;
            const comandToEsp = document.getElementById('edit-esp-comandToEsp').value;
            const status = document.getElementById('edit-esp-status').value;
            const ligado = document.getElementById('edit-esp-ligado').checked;

            try {
                const response = await fetch(`${API_BASE_URL}/api/DispositivosEsp/${id}`, {
                    method: 'PUT',
                    headers: {
                        'Content-Type': 'application/json',
                        'Authorization': `Bearer ${authToken}`
                    },
                    body: JSON.stringify({ nome, ip, porta, comando, comandToEsp, status, ligadoDesligado: ligado })
                });

                if (!response.ok) throw new Error('Erro ao atualizar dispositivo ESP');

                alert('Dispositivo ESP atualizado com sucesso!');
                document.getElementById('edit-dispositivo-esp-modal').style.display = 'none';
                loadUserDispositivosEsp();
            } catch (error) {
                alert('Erro: ' + error.message);
            }
        });
    }

    // Enviar Comando Form
    const enviarComandoForm = document.getElementById('enviar-comando-esp-form');
    if (enviarComandoForm) {
        enviarComandoForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            const comando = document.getElementById('comando-esp-texto').value;

            try {
                const response = await fetch(`${API_BASE_URL}/api/DispositivosEsp/enviar-comando`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'Authorization': `Bearer ${authToken}`
                    },
                    body: JSON.stringify({ comando })
                });

                if (!response.ok) {
                    const error = await response.json();
                    throw new Error(error || 'Erro ao enviar comando');
                }

                const data = await response.json();
                alert(`Comando enviado para ${data.dispositivo.nome}!`);
                document.getElementById('enviar-comando-esp-modal').style.display = 'none';
                
                // O backend já envia o comando via WebSocket diretamente para o grupo 'type_software'
                // Não é necessário chamar EnviarComandoParaSoftware novamente
            } catch (error) {
                alert('Erro: ' + error.message);
            }
        });
    }
}

// Load User Agendamentos
async function loadUserAgendamentos() {
    try {
        const response = await fetchWithAuth(`${API_BASE_URL}/api/Agendamentos`);
        const agendamentosList = document.getElementById('agendamentos-list');

        if (!response.ok) {
            // Se for 403 (Forbidden), mostrar mensagem genérica
            if (response.status === 403) {
                agendamentosList.innerHTML = '<div class="empty-state"><div class="empty-state-icon">⏰</div><p>Você não tem permissão para acessar agendamentos</p></div>';
                return;
            }
            
            // Para outros erros, tentar ler a mensagem de erro se houver
            let errorMessage = 'Erro ao carregar agendamentos';
            try {
                const errorText = await response.text();
                if (errorText) {
                    try {
                        const errorJson = JSON.parse(errorText);
                        errorMessage = errorJson.message || errorJson.error || errorMessage;
                    } catch {
                        errorMessage = errorText || errorMessage;
                    }
                }
            } catch {
                // Se não conseguir ler o erro, usar mensagem padrão
            }
            
            agendamentosList.innerHTML = `<div class="error-message show">${errorMessage}</div>`;
            return;
        }

        // Verificar se a resposta tem conteúdo antes de fazer parse
        const responseText = await response.text();
        if (!responseText || responseText.trim() === '') {
            agendamentosList.innerHTML = '<div class="empty-state"><div class="empty-state-icon">⏰</div><p>Nenhum agendamento cadastrado</p></div>';
            return;
        }

        const agendamentos = JSON.parse(responseText);

        if (agendamentos.length === 0) {
            agendamentosList.innerHTML = '<div class="empty-state"><div class="empty-state-icon">⏰</div><p>Nenhum agendamento cadastrado</p></div>';
            return;
        }
        agendamentosList.innerHTML = agendamentos.map(ag => {
            const dataHora = new Date(ag.agendadoPara);
            const dataFormatada = dataHora.toLocaleDateString('pt-BR');
            const horaFormatada = dataHora.toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' });
            
            // Determinar tipo de agendamento
            // O enum vem como número: 1=Starkswitch, 2=ESP, 3=Ewelink
            const tipoNum = ag.tipoAgendamento;
            let tipo = 'Desconhecido';
            if (tipoNum === 1 || tipoNum === 'Starkswitch' || String(tipoNum).toLowerCase() === 'starkswitch') {
                tipo = 'Starkswitch';
            } else if (tipoNum === 2 || tipoNum === 'ESP' || String(tipoNum).toLowerCase() === 'esp') {
                tipo = 'ESP';
            } else if (tipoNum === 3 || tipoNum === 'Ewelink' || String(tipoNum).toLowerCase() === 'ewelink') {
                tipo = 'Ewelink';
            }
            const status = ag.executado ? 'Executado' : 'Pendente';
            const recorrencia = ag.recorrencia || 'Não Repetir';
            
            return `
                <div class="item-card">
                    <div class="item-card-header">
                        <div class="item-card-title">Agendamento ${tipo}</div>
                        <div class="item-card-actions">
                            <button class="btn btn-secondary" onclick="deleteUserAgendamento('${ag.id}')" style="background: rgba(239, 68, 68, 0.2); color: var(--error-color);">Excluir</button>
                        </div>
                    </div>
                    <div class="item-card-body">
                        <p><strong>Tipo:</strong> ${tipo}</p>
                        <p><strong>Data/Hora:</strong> ${dataFormatada} às ${horaFormatada}</p>
                        <p><strong>Comando:</strong> ${escapeHtml(ag.comando || 'N/A')}</p>
                        <p><strong>Recorrência:</strong> ${recorrencia}</p>
                        <p><strong>Status:</strong> <span class="agendamento-status ${ag.executado ? 'executado' : 'pendente'}">${status}</span></p>
                    </div>
                </div>
            `;
        }).join('');
    } catch (error) {
        console.error('Erro ao carregar agendamentos:', error);
        document.getElementById('agendamentos-list').innerHTML = '<div class="error-message show">Erro ao carregar agendamentos</div>';
    }
}

// Load Planos Ativos
async function loadPlanosAtivos() {
    console.log('🔍 [Planos Ativos] Função chamada');
    try {
        console.log('🔍 [Planos Ativos] Iniciando carregamento...');
        
        // Aguardar um pouco para garantir que o DOM está pronto
        await new Promise(resolve => setTimeout(resolve, 50));
        
        const planosList = document.getElementById('planos-ativos-list');
        console.log('🔍 [Planos Ativos] Elemento encontrado:', planosList ? 'SIM' : 'NÃO');
        
        if (!planosList) {
            console.error('❌ [Planos Ativos] Elemento planos-ativos-list não encontrado! Tentando novamente em 500ms...');
            // Tentar novamente após um delay maior
            setTimeout(async () => {
                await loadPlanosAtivos();
            }, 500);
            return;
        }

        console.log('🔍 [Planos Ativos] Elemento encontrado, fazendo requisição...');
        const response = await fetchWithAuth(`${API_BASE_URL}/api/Assinaturas/ativas`);
        console.log('✅ [Planos Ativos] Resposta recebida:', response.status, response.statusText);

        if (!response.ok) {
            let errorMessage = 'Erro ao carregar planos ativos';
            try {
                const errorText = await response.text();
                console.error('❌ [Planos Ativos] Erro na resposta:', errorText);
                if (errorText) {
                    try {
                        const errorJson = JSON.parse(errorText);
                        errorMessage = errorJson.message || errorJson.error || errorMessage;
                    } catch {
                        errorMessage = errorText || errorMessage;
                    }
                }
            } catch (e) {
                console.error('❌ [Planos Ativos] Erro ao processar mensagem de erro:', e);
            }
            
            planosList.innerHTML = `<div class="error-message show">${errorMessage}</div>`;
            return;
        }

        const responseText = await response.text();
        console.log('📄 [Planos Ativos] Texto da resposta:', responseText);
        
        if (!responseText || responseText.trim() === '') {
            console.log('⚠️ [Planos Ativos] Resposta vazia, mostrando estado vazio');
            planosList.innerHTML = '<div class="empty-state"><div class="empty-state-icon">💳</div><p>Nenhum plano ativo encontrado</p><button class="btn btn-primary" onclick="openPlanoModal()" style="margin-top: 1rem;">Contratar Plano</button></div>';
            return;
        }

        const planos = JSON.parse(responseText);
        console.log('📋 [Planos Ativos] Planos parseados:', planos);

        if (planos.length === 0) {
            planosList.innerHTML = '<div class="empty-state"><div class="empty-state-icon">💳</div><p>Nenhum plano ativo encontrado</p><button class="btn btn-primary" onclick="openPlanoModal()" style="margin-top: 1rem;">Contratar Plano</button></div>';
            return;
        }

        planosList.innerHTML = planos.map(plano => {
            const dataInicio = plano.iniciadaEm ? new Date(plano.iniciadaEm).toLocaleDateString('pt-BR') : 'N/A';
            const dataExpiracao = plano.expiraEm ? new Date(plano.expiraEm).toLocaleDateString('pt-BR') : 'Sem expiração';
            const dataCriacao = new Date(plano.dataCriacao).toLocaleDateString('pt-BR');
            
            // Determinar cor do badge baseado no nível
            let badgeColor = 'var(--primary-color)';
            if (plano.nivel === 2) {
                badgeColor = '#10b981'; // Verde para Remove Ads
            } else if (plano.nivel >= 3 && plano.nivel <= 7) {
                badgeColor = '#3b82f6'; // Azul para planos de StarkCoins
            }
            
            return `
                <div class="item-card">
                    <div class="item-card-header">
                        <div class="item-card-title">${plano.nomePlano}</div>
                        <div class="item-card-actions">
                            <span class="badge" style="background: ${badgeColor}; color: white; padding: 0.25rem 0.75rem; border-radius: 0.25rem; font-size: 0.875rem; font-weight: 600;">${plano.status}</span>
                            <button class="btn btn-secondary" onclick="cancelarPlano('${plano.id}')" style="background: rgba(239, 68, 68, 0.2); color: var(--error-color); margin-left: 0.5rem;">Cancelar Plano</button>
                        </div>
                    </div>
                    <div class="item-card-body">
                        <div class="info-row">
                            <span class="info-label">Nível:</span>
                            <span class="info-value">Nível ${plano.nivel}</span>
                        </div>
                        <div class="info-row">
                            <span class="info-label">Valor:</span>
                            <span class="info-value">R$ ${plano.valor.toFixed(2)}/mês</span>
                        </div>
                        <div class="info-row">
                            <span class="info-label">Iniciado em:</span>
                            <span class="info-value">${dataInicio}</span>
                        </div>
                        <div class="info-row">
                            <span class="info-label">Expira em:</span>
                            <span class="info-value">${dataExpiracao}</span>
                        </div>
                        <div class="info-row">
                            <span class="info-label">Criado em:</span>
                            <span class="info-value">${dataCriacao}</span>
                        </div>
                        ${plano.stripeSubscriptionId ? `
                        <div class="info-row">
                            <span class="info-label">ID Stripe:</span>
                            <span class="info-value" style="font-size: 0.75rem; color: var(--text-secondary);">${plano.stripeSubscriptionId}</span>
                        </div>
                        ` : ''}
                    </div>
                </div>
            `;
        }).join('');
        console.log('✅ [Planos Ativos] Planos renderizados com sucesso');
    } catch (error) {
        console.error('❌ [Planos Ativos] Erro ao carregar planos ativos:', error);
        console.error('❌ [Planos Ativos] Stack:', error.stack);
        const planosList = document.getElementById('planos-ativos-list');
        if (planosList) {
            planosList.innerHTML = `<div class="error-message show">Erro: ${error.message}</div>`;
        } else {
            console.error('❌ [Planos Ativos] Elemento planos-ativos-list não existe para mostrar erro!');
        }
    }
}

// Cancelar Plano
async function cancelarPlano(assinaturaId) {
    if (!confirm('Tem certeza que deseja cancelar este plano? O cancelamento será processado imediatamente.')) {
        return;
    }

    try {
        console.log('🛑 [Planos Ativos] Cancelando assinatura:', assinaturaId);
        const response = await fetchWithAuth(`${API_BASE_URL}/api/Assinaturas/cancelar/${assinaturaId}`, {
            method: 'POST'
        });

        if (!response.ok) {
            let errorMessage = 'Erro ao cancelar plano';
            try {
                const errorText = await response.text();
                if (errorText) {
                    try {
                        const errorJson = JSON.parse(errorText);
                        errorMessage = errorJson.message || errorJson.error || errorMessage;
                    } catch {
                        errorMessage = errorText || errorMessage;
                    }
                }
            } catch {
                // Se não conseguir ler o erro, usar mensagem padrão
            }
            alert('Erro ao cancelar plano: ' + errorMessage);
            return;
        }

        const result = await response.json();
        console.log('✅ [Planos Ativos] Plano cancelado:', result);
        
        alert('Plano cancelado com sucesso!');
        
        // Recarregar a lista de planos ativos
        await loadPlanosAtivos();
    } catch (error) {
        console.error('❌ [Planos Ativos] Erro ao cancelar plano:', error);
        alert('Erro ao cancelar plano: ' + error.message);
    }
}

// Open Criar Agendamento ESP Modal
async function openCriarAgendamentoEspModal() {
    const modal = document.getElementById('criar-agendamento-esp-modal');
    const select = document.getElementById('agendamento-esp-dispositivo');
    
    // Carregar dispositivos ESP
    try {
        const response = await fetchWithAuth(`${API_BASE_URL}/api/DispositivosEsp`);
        if (response.ok) {
            const dispositivos = await response.json();
            select.innerHTML = '<option value="">Selecione um dispositivo ESP</option>';
            dispositivos.forEach(d => {
                const option = document.createElement('option');
                option.value = d.id;
                option.textContent = d.nome;
                select.appendChild(option);
            });
        }
    } catch (error) {
        console.error('Erro ao carregar dispositivos ESP:', error);
    }
    
    // Set today's date as default
    const today = new Date().toISOString().split('T')[0];
    document.getElementById('agendamento-esp-data').value = today;
    document.getElementById('agendamento-esp-hora').value = '';
    document.getElementById('agendamento-esp-minuto').value = '';
    
    modal.style.display = 'block';
}

// Open Criar Agendamento Starkswitch Modal
async function openCriarAgendamentoStarkswitchModal() {
    const modal = document.getElementById('criar-agendamento-starkswitch-modal');
    const select = document.getElementById('agendamento-starkswitch-dispositivo');
    
    // Carregar dispositivos Starkswitch
    try {
        const response = await fetchWithAuth(`${API_BASE_URL}/api/devices`);
        if (response.ok) {
            const dispositivos = await response.json();
            select.innerHTML = '<option value="">Selecione um dispositivo Starkswitch</option>';
            dispositivos.forEach(d => {
                const option = document.createElement('option');
                option.value = d.id;
                option.textContent = d.name;
                select.appendChild(option);
            });
        }
    } catch (error) {
        console.error('Erro ao carregar dispositivos Starkswitch:', error);
    }
    
    // Set today's date as default
    const today = new Date().toISOString().split('T')[0];
    document.getElementById('agendamento-starkswitch-data').value = today;
    document.getElementById('agendamento-starkswitch-hora').value = '';
    document.getElementById('agendamento-starkswitch-minuto').value = '';
    
    modal.style.display = 'block';
}

// Open Criar Agendamento Ewelink Modal
async function openCriarAgendamentoEwelinkModal() {
    const modal = document.getElementById('criar-agendamento-ewelink-modal');
    const select = document.getElementById('agendamento-ewelink-dispositivo');
    
    // Carregar dispositivos Ewelink
    try {
        const response = await fetchWithAuth(`${API_BASE_URL}/api/Ewelink/dispositivos`);
        if (response.ok) {
            const dispositivos = await response.json();
            select.innerHTML = '<option value="">Selecione um dispositivo Ewelink</option>';
            dispositivos.forEach(d => {
                const option = document.createElement('option');
                option.value = d.deviceId;
                option.textContent = d.name;
                select.appendChild(option);
            });
        }
    } catch (error) {
        console.error('Erro ao carregar dispositivos Ewelink:', error);
        showNotification('Erro ao carregar dispositivos Ewelink. Certifique-se de estar conectado.', 'error');
    }
    
    // Set today's date as default
    const today = new Date().toISOString().split('T')[0];
    document.getElementById('agendamento-ewelink-data').value = today;
    document.getElementById('agendamento-ewelink-hora').value = '';
    document.getElementById('agendamento-ewelink-minuto').value = '';
    
    modal.style.display = 'block';
}

// Delete User Agendamento
async function deleteUserAgendamento(agendamentoId) {
    if (!confirm('Tem certeza que deseja excluir este agendamento?')) return;

    try {
        const response = await fetch(`${API_BASE_URL}/api/Agendamentos/${agendamentoId}`, {
            method: 'DELETE',
            headers: {
                'Authorization': `Bearer ${authToken}`
            }
        });

        if (!response.ok) throw new Error('Erro ao excluir agendamento');

        alert('Agendamento excluído com sucesso!');
        loadUserAgendamentos();
    } catch (error) {
        alert('Erro: ' + error.message);
    }
}

// Setup Agendamentos Forms
function setupAgendamentosForms() {
    // Criar Agendamento ESP Form
    const criarAgendamentoEspForm = document.getElementById('criar-agendamento-esp-form');
    if (criarAgendamentoEspForm) {
        criarAgendamentoEspForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            const dispositivoEspId = document.getElementById('agendamento-esp-dispositivo').value;
            const data = document.getElementById('agendamento-esp-data').value;
            const hora = parseInt(document.getElementById('agendamento-esp-hora').value);
            const minuto = parseInt(document.getElementById('agendamento-esp-minuto').value);
            const recorrencia = document.getElementById('agendamento-esp-recorrencia').value;

            if (!dispositivoEspId) {
                alert('Selecione um dispositivo ESP');
                return;
            }

            try {
                const response = await fetch(`${API_BASE_URL}/api/Agendamentos/esp`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'Authorization': `Bearer ${authToken}`
                    },
                    body: JSON.stringify({
                        dispositivoEspId: dispositivoEspId,
                        data: data,
                        hora: hora,
                        minuto: minuto,
                        recorrencia: recorrencia
                    })
                });

                if (!response.ok) {
                    let errorMessage = 'Erro ao criar agendamento';
                    if (response.status === 403) {
                        errorMessage = 'Você não tem permissão para criar agendamentos';
                    } else {
                        try {
                            const errorText = await response.text();
                            if (errorText) {
                                try {
                                    const errorJson = JSON.parse(errorText);
                                    errorMessage = errorJson.message || errorJson.error || errorMessage;
                                } catch {
                                    errorMessage = errorText || errorMessage;
                                }
                            }
                        } catch {
                            // Usar mensagem padrão
                        }
                    }
                    throw new Error(errorMessage);
                }

                alert('Agendamento ESP criado com sucesso!');
                document.getElementById('criar-agendamento-esp-modal').style.display = 'none';
                criarAgendamentoEspForm.reset();
                loadUserAgendamentos();
            } catch (error) {
                alert('Erro: ' + error.message);
            }
        });
    }

    // Criar Agendamento Starkswitch Form
    const criarAgendamentoStarkswitchForm = document.getElementById('criar-agendamento-starkswitch-form');
    if (criarAgendamentoStarkswitchForm) {
        criarAgendamentoStarkswitchForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            const deviceId = document.getElementById('agendamento-starkswitch-dispositivo').value;
            const acao = document.getElementById('agendamento-starkswitch-acao').value;
            const data = document.getElementById('agendamento-starkswitch-data').value;
            const hora = parseInt(document.getElementById('agendamento-starkswitch-hora').value);
            const minuto = parseInt(document.getElementById('agendamento-starkswitch-minuto').value);
            const recorrencia = document.getElementById('agendamento-starkswitch-recorrencia').value;

            if (!deviceId) {
                alert('Selecione um dispositivo Starkswitch');
                return;
            }

            try {
                const response = await fetch(`${API_BASE_URL}/api/Agendamentos/starkswitch`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'Authorization': `Bearer ${authToken}`
                    },
                    body: JSON.stringify({
                        deviceId: deviceId,
                        acao: acao,
                        data: data,
                        hora: hora,
                        minuto: minuto,
                        recorrencia: recorrencia
                    })
                });

                if (!response.ok) {
                    let errorMessage = 'Erro ao criar agendamento';
                    if (response.status === 403) {
                        errorMessage = 'Você não tem permissão para criar agendamentos';
                    } else {
                        try {
                            const errorText = await response.text();
                            if (errorText) {
                                try {
                                    const errorJson = JSON.parse(errorText);
                                    errorMessage = errorJson.message || errorJson.error || errorMessage;
                                } catch {
                                    errorMessage = errorText || errorMessage;
                                }
                            }
                        } catch {
                            // Usar mensagem padrão
                        }
                    }
                    throw new Error(errorMessage);
                }

                alert('Agendamento Starkswitch criado com sucesso!');
                document.getElementById('criar-agendamento-starkswitch-modal').style.display = 'none';
                criarAgendamentoStarkswitchForm.reset();
                loadUserAgendamentos();
            } catch (error) {
                alert('Erro: ' + error.message);
            }
        });
    }

    // Criar Agendamento Ewelink Form
    const criarAgendamentoEwelinkForm = document.getElementById('criar-agendamento-ewelink-form');
    if (criarAgendamentoEwelinkForm) {
        criarAgendamentoEwelinkForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            const ewelinkDeviceId = document.getElementById('agendamento-ewelink-dispositivo').value;
            const acao = document.getElementById('agendamento-ewelink-acao').value;
            const data = document.getElementById('agendamento-ewelink-data').value;
            const hora = parseInt(document.getElementById('agendamento-ewelink-hora').value);
            const minuto = parseInt(document.getElementById('agendamento-ewelink-minuto').value);
            const recorrencia = document.getElementById('agendamento-ewelink-recorrencia').value;

            if (!ewelinkDeviceId) {
                alert('Selecione um dispositivo Ewelink');
                return;
            }

            try {
                const response = await fetchWithAuth(`${API_BASE_URL}/api/Agendamentos/ewelink`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({
                        ewelinkDeviceId: ewelinkDeviceId,
                        acao: acao,
                        data: data,
                        hora: hora,
                        minuto: minuto,
                        recorrencia: recorrencia
                    })
                });

                if (!response.ok) {
                    const error = await response.json();
                    throw new Error(error.message || 'Erro ao criar agendamento');
                }

                showNotification('Agendamento Ewelink criado com sucesso!', 'success');
                document.getElementById('criar-agendamento-ewelink-modal').style.display = 'none';
                criarAgendamentoEwelinkForm.reset();
                loadUserAgendamentos();
            } catch (error) {
                console.error('Erro ao criar agendamento Ewelink:', error);
                showNotification('Erro ao criar agendamento: ' + error.message, 'error');
            }
        });
    }
}

// Request Password Reset
async function requestPasswordReset() {
    const email = currentUser?.email || document.getElementById('login-email')?.value || '';
    
    if (!email) {
        const emailInput = prompt('Por favor, digite seu email:');
        if (!emailInput) return;
        
        try {
            const response = await fetch(`${API_BASE_URL}/api/Users/request-password-reset`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ Email: emailInput })
            });
            
            const message = await response.text();
            
            if (response.ok) {
                alert('Instruções para redefinir sua senha foram enviadas para seu email.\n\nVerifique sua caixa de entrada e siga as instruções.');
            } else {
                alert('Erro ao enviar email de redefinição de senha.\n\nVerifique se o email está correto e tente novamente.');
            }
        } catch (error) {
            alert('Erro: ' + error.message);
        }
        return;
    }
    
    if (!confirm(`Deseja enviar um email de redefinição de senha para ${email}?`)) {
        return;
    }
    
    try {
        const response = await fetch(`${API_BASE_URL}/api/Users/request-password-reset`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ Email: email })
        });
        
        const message = await response.text();
        
        if (response.ok) {
            alert('Instruções para redefinir sua senha foram enviadas para seu email.\n\nVerifique sua caixa de entrada e siga as instruções.');
        } else {
            alert('Erro ao enviar email de redefinição de senha.\n\nVerifique se o email está correto e tente novamente.');
        }
    } catch (error) {
        alert('Erro: ' + error.message);
    }
}

// Logout
function logout() {
    // Limpar intervalo de notificações
    if (notificationsInterval) {
        clearInterval(notificationsInterval);
        notificationsInterval = null;
    }
    localStorage.removeItem('authToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('currentUser');
    
    authToken = null;
    refreshToken = null;
    currentUser = null;
    
    document.getElementById('auth-section').style.display = 'block';
    document.getElementById('dashboard-section').style.display = 'none';
    document.getElementById('login-email').value = '';
    document.getElementById('login-password').value = '';
    
    updateAuthMenu();
}

// Download Windows Software
function downloadWindowsSoftware() {
    window.location.href = 'https://starkaid.runasp.net/soft-dowload/starckaidautomacao.exe';
}

// Carregar Licenças do Usuário
async function loadUserLicenses() {
    const licensesListContainer = document.getElementById('user-licenses-list');
    
    if (!licensesListContainer) {
        console.error('Container de licenças não encontrado');
        return;
    }

    if (!authToken) {
        licensesListContainer.innerHTML = '<div style="text-align: center; padding: 1rem; color: var(--light-text);">Faça login para ver suas licenças.</div>';
        return;
    }

    try {
        licensesListContainer.innerHTML = '<div style="text-align: center; padding: 1rem; color: var(--light-text);">Carregando licenças...</div>';

        const response = await fetchWithAuth(`${API_BASE_URL}/api/licenses`);

        if (!response.ok) {
            if (response.status === 401) {
                licensesListContainer.innerHTML = '<div style="text-align: center; padding: 1rem; color: var(--error-color);">Sessão expirada. Faça login novamente.</div>';
                return;
            }
            throw new Error(`Erro ao carregar licenças: ${response.status}`);
        }

        const licenses = await response.json();
        displayUserLicenses(licenses);
    } catch (error) {
        console.error('Erro ao carregar licenças:', error);
        licensesListContainer.innerHTML = `<div style="text-align: center; padding: 1rem; color: var(--error-color);">Erro ao carregar licenças: ${error.message}</div>`;
    }
}

// Exibir Licenças do Usuário
function displayUserLicenses(licenses) {
    const licensesListContainer = document.getElementById('user-licenses-list');
    
    if (!licensesListContainer) {
        return;
    }

    if (!licenses || licenses.length === 0) {
        licensesListContainer.innerHTML = '<div style="text-align: center; padding: 1rem; color: var(--light-text);">Você ainda não possui licenças.</div>';
        return;
    }

    licensesListContainer.innerHTML = licenses.map(license => {
        const licenseKey = license.licenseKey || license.LicenseKey || '';
        const isActive = license.isActive !== undefined ? license.isActive : license.IsActive;
        const maxMachines = license.maxMachines || license.MaxMachines || 0;
        const activeActivations = license.activeActivations || license.ActiveActivations || 0;
        const price = license.price || license.Price || 0;
        const createdAt = license.createdAt || license.CreatedAt;
        const activations = license.activations || license.Activations || [];

        const formattedDate = new Date(createdAt).toLocaleDateString('pt-BR');

        return `
            <div style="width: 100%; background: rgba(0, 255, 255, 0.1); border: 1px solid rgba(0, 255, 255, 0.3); border-radius: 8px; padding: 1rem; margin-bottom: 1rem; box-sizing: border-box;">
                <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 1rem; flex-wrap: wrap; gap: 0.5rem;">
                    <div style="font-family: 'Courier New', monospace; font-size: 1.1rem; font-weight: bold; color: var(--primary-color);">
                        ${licenseKey}
                    </div>
                    <span style="background: ${isActive ? 'linear-gradient(135deg, #00ff00 0%, #00cc00 100%)' : 'linear-gradient(135deg, #ff4444 0%, #cc0000 100%)'}; 
                               color: ${isActive ? '#000' : '#fff'}; 
                               padding: 0.25rem 0.75rem; 
                               border-radius: 4px; 
                               font-size: 0.85rem; 
                               font-weight: bold;">
                        ${isActive ? 'ATIVA' : 'INATIVA'}
                    </span>
                </div>
                
                <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(150px, 1fr)); gap: 1rem; margin-bottom: 1rem;">
                    <div>
                        <div style="color: var(--light-text); font-size: 0.9rem; margin-bottom: 0.25rem;">Máquinas Permitidas</div>
                        <div style="color: #fff; font-weight: bold; font-size: 1.1rem;">${maxMachines}</div>
                    </div>
                    <div>
                        <div style="color: var(--light-text); font-size: 0.9rem; margin-bottom: 0.25rem;">Máquinas Ativas</div>
                        <div style="color: #fff; font-weight: bold; font-size: 1.1rem;">${activeActivations} / ${maxMachines}</div>
                    </div>
                    <div>
                        <div style="color: var(--light-text); font-size: 0.9rem; margin-bottom: 0.25rem;">Preço Pago</div>
                        <div style="color: #fff; font-weight: bold; font-size: 1.1rem;">R$ ${price.toFixed(2)}</div>
                    </div>
                    <div>
                        <div style="color: var(--light-text); font-size: 0.9rem; margin-bottom: 0.25rem;">Data de Compra</div>
                        <div style="color: #fff; font-weight: bold; font-size: 1.1rem;">${formattedDate}</div>
                    </div>
                </div>

                ${activations.length > 0 ? `
                    <div style="margin-top: 1rem; padding-top: 1rem; border-top: 1px solid rgba(0, 255, 255, 0.2);">
                        <div style="color: var(--primary-color); font-weight: bold; margin-bottom: 0.75rem; font-size: 1rem;">Máquinas Ativadas</div>
                        ${activations.map(activation => {
                            const machineName = activation.machineName || activation.MachineName || activation.machineId || activation.MachineId || 'N/A';
                            const activatedAt = activation.activatedAt || activation.ActivatedAt;
                            const activationIsActive = activation.isActive !== undefined ? activation.isActive : activation.IsActive;
                            const formattedActivationDate = new Date(activatedAt).toLocaleDateString('pt-BR');
                            
                            return `
                                <div style="display: flex; justify-content: space-between; align-items: center; 
                                           background: rgba(0, 255, 255, 0.05); 
                                           padding: 0.75rem; 
                                           border-radius: 6px; 
                                           margin-bottom: 0.5rem;">
                                    <div>
                                        <div style="color: #fff; font-weight: bold; margin-bottom: 0.25rem;">${machineName}</div>
                                        <div style="color: var(--light-text); font-size: 0.85rem;">Ativada em ${formattedActivationDate}</div>
                                    </div>
                                    <span style="background: ${activationIsActive ? 'linear-gradient(135deg, #00ff00 0%, #00cc00 100%)' : 'linear-gradient(135deg, #ff4444 0%, #cc0000 100%)'}; 
                                               color: ${activationIsActive ? '#000' : '#fff'}; 
                                               padding: 0.25rem 0.75rem; 
                                               border-radius: 4px; 
                                               font-size: 0.8rem; 
                                               font-weight: bold;">
                                        ${activationIsActive ? 'ATIVA' : 'INATIVA'}
                                    </span>
                                </div>
                            `;
                        }).join('')}
                    </div>
                ` : ''}
            </div>
        `;
    }).join('');
}

// ==================== FUNÇÕES EWELINK ====================

// Verificar status do login Ewelink
async function checkEwelinkStatus() {
    const statusMessage = document.getElementById('ewelink-status-message');
    const devicesList = document.getElementById('ewelink-devices-list');
    const loginBtn = document.getElementById('ewelink-login-btn');
    const syncBtn = document.getElementById('ewelink-sync-btn');
    const logoutBtn = document.getElementById('ewelink-logout-btn');
    
    devicesList.innerHTML = '<div class="loading-spinner">Verificando status...</div>';
    
    try {
        const response = await fetchWithAuth(`${API_BASE_URL}/api/Ewelink/status`);
        
        if (!response.ok) {
            // Se for erro 500 ou outro erro, tratar como não logado
            const errorText = await response.text();
            console.error('Erro ao verificar status:', response.status, errorText);
            throw new Error('Erro ao verificar status');
        }
        
        const data = await response.json();
        
        if (data.isLoggedIn) {
            // Usuário está logado
            if (statusMessage) {
                statusMessage.style.display = 'block';
                statusMessage.innerHTML = '<div style="color: #00ff00;">✓ Conectado ao Ewelink - Tokens serão atualizados automaticamente</div>';
                statusMessage.style.background = 'rgba(0, 255, 0, 0.1)';
            }
            
            if (loginBtn) loginBtn.style.display = 'none';
            if (syncBtn) syncBtn.style.display = 'inline-block';
            if (logoutBtn) logoutBtn.style.display = 'inline-block';
            
            // Carregar dispositivos
            await loadEwelinkDevices();
        } else {
            // Usuário não está logado
            if (statusMessage) {
                statusMessage.style.display = 'block';
                statusMessage.innerHTML = '<div style="color: #ffaa00;">⚠ Não conectado ao Ewelink. Faça login para começar.</div>';
                statusMessage.style.background = 'rgba(255, 170, 0, 0.1)';
            }
            
            if (loginBtn) {
                loginBtn.style.display = 'inline-block';
                loginBtn.textContent = '🔐 Conectar Conta Ewelink';
            }
            if (syncBtn) syncBtn.style.display = 'none';
            if (logoutBtn) logoutBtn.style.display = 'none';
            
            devicesList.innerHTML = `
                <div class="empty-state">
                    <div class="empty-state-icon" style="font-size: 4rem; margin-bottom: 1rem;">🔌</div>
                    <h3 style="margin: 1rem 0; color: var(--primary-color);">Conecte sua conta Ewelink</h3>
                    <p style="margin-bottom: 1.5rem; color: var(--light-text); max-width: 500px; margin-left: auto; margin-right: auto;">
                        Para gerenciar seus dispositivos Ewelink, você precisa conectar sua conta primeiro. 
                        Clique no botão abaixo para autorizar o acesso.
                    </p>
                    <button class="btn btn-primary" onclick="openEwelinkLoginModal()" style="padding: 0.75rem 2rem; font-size: 1rem; margin-bottom: 1rem;">
                        🔐 Conectar Conta Ewelink
                    </button>
                    <p style="margin-top: 1rem; font-size: 0.85rem; color: var(--light-text); opacity: 0.8;">
                        Você será redirecionado para a página de autorização do Ewelink
                    </p>
                </div>
            `;
        }
    } catch (error) {
        console.error('Erro ao verificar status Ewelink:', error);
        
        // Se houver erro, mostrar opção de login
        if (statusMessage) {
            statusMessage.style.display = 'block';
            statusMessage.innerHTML = '<div style="color: #ffaa00;">⚠ Não conectado ao Ewelink. Faça login para começar.</div>';
            statusMessage.style.background = 'rgba(255, 170, 0, 0.1)';
        }
        
        if (loginBtn) loginBtn.style.display = 'inline-block';
        if (syncBtn) syncBtn.style.display = 'none';
        if (logoutBtn) logoutBtn.style.display = 'none';
        
        devicesList.innerHTML = `
            <div class="empty-state">
                <div class="empty-state-icon">🔌</div>
                <h3 style="margin: 1rem 0;">Conecte sua conta Ewelink</h3>
                <p style="margin-bottom: 1.5rem;">Para gerenciar seus dispositivos Ewelink, você precisa conectar sua conta primeiro.</p>
                <button class="btn btn-primary" onclick="openEwelinkLoginModal()" style="padding: 0.75rem 2rem; font-size: 1rem;">
                    🔐 Conectar Conta Ewelink
                </button>
                <p style="margin-top: 1rem; font-size: 0.9rem; color: var(--light-text);">
                    Você será redirecionado para a página de autorização do Ewelink
                </p>
            </div>
        `;
    }
}

// Carregar dispositivos Ewelink
async function loadEwelinkDevices() {
    const devicesList = document.getElementById('ewelink-devices-list');
    
    try {
        const response = await fetchWithAuth(`${API_BASE_URL}/api/Ewelink/dispositivos`);
        
        if (!response.ok) {
            throw new Error('Erro ao carregar dispositivos');
        }
        
        const devices = await response.json();
        
        if (devices.length === 0) {
            devicesList.innerHTML = '<div class="empty-state"><div class="empty-state-icon">🔌</div><p>Nenhum dispositivo encontrado</p><button class="btn btn-primary" onclick="syncEwelinkDevices()" style="margin-top: 1rem;">Sincronizar Dispositivos</button></div>';
            return;
        }
        
        devicesList.innerHTML = devices.map(device => {
            const statusClass = device.online ? 'online' : 'offline';
            const statusText = device.online ? 'Online' : 'Offline';
            const switchStatus = device.isOn ? 'Ligado' : 'Desligado';
            const switchClass = device.isOn ? 'on' : 'off';
            
            return `
                <div class="item-card">
                    <div class="item-card-header">
                        <div class="item-card-title">${escapeHtml(device.name)}</div>
                        <div class="item-card-actions">
                            <span class="status-badge ${statusClass}">${statusText}</span>
                        </div>
                    </div>
                    <div class="item-card-body">
                        <p><strong>Status:</strong> <span class="switch-status ${switchClass}">${switchStatus}</span></p>
                        <p><strong>ID:</strong> <code style="font-size: 0.8rem;">${device.deviceId}</code></p>
                        <p><strong>Tipo:</strong> ${device.type}</p>
                        <div style="margin-top: 1rem; display: flex; gap: 0.5rem;">
                            <button class="btn btn-primary" onclick="controlEwelinkDevice('${device.deviceId}', true)" ${!device.online ? 'disabled' : ''}>
                                🔛 Ligar
                            </button>
                            <button class="btn btn-secondary" onclick="controlEwelinkDevice('${device.deviceId}', false)" ${!device.online ? 'disabled' : ''}>
                                🔴 Desligar
                            </button>
                            <button class="btn btn-secondary" onclick="refreshEwelinkDeviceStatus('${device.deviceId}')">
                                🔄 Atualizar
                            </button>
                        </div>
                    </div>
                </div>
            `;
        }).join('');
    } catch (error) {
        console.error('Erro ao carregar dispositivos Ewelink:', error);
        devicesList.innerHTML = '<div class="error-message show">Erro ao carregar dispositivos: ' + error.message + '</div>';
    }
}

// Controlar dispositivo Ewelink (ligar/desligar)
async function controlEwelinkDevice(deviceId, switchOn) {
    try {
        const response = await fetchWithAuth(`${API_BASE_URL}/api/Ewelink/dispositivos/${deviceId}/controlar`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ Switch: switchOn })
        });
        
        if (!response.ok) {
            const error = await response.json();
            throw new Error(error.message || 'Erro ao controlar dispositivo');
        }
        
        const device = await response.json();
        
        // Atualizar apenas o dispositivo específico na lista
        await refreshEwelinkDeviceStatus(deviceId);
        
        // Mostrar feedback visual
        const action = switchOn ? 'ligado' : 'desligado';
        showNotification(`Dispositivo ${action} com sucesso!`, 'success');
    } catch (error) {
        console.error('Erro ao controlar dispositivo:', error);
        showNotification('Erro ao controlar dispositivo: ' + error.message, 'error');
    }
}

// Atualizar status de um dispositivo específico
async function refreshEwelinkDeviceStatus(deviceId) {
    try {
        const response = await fetchWithAuth(`${API_BASE_URL}/api/Ewelink/dispositivos/${deviceId}/status`);
        
        if (!response.ok) {
            throw new Error('Erro ao atualizar status');
        }
        
        const device = await response.json();
        
        // Recarregar todos os dispositivos para atualizar a lista
        await loadEwelinkDevices();
    } catch (error) {
        console.error('Erro ao atualizar status do dispositivo:', error);
    }
}

// Sincronizar dispositivos Ewelink
async function syncEwelinkDevices() {
    const devicesList = document.getElementById('ewelink-devices-list');
    const syncBtn = document.getElementById('ewelink-sync-btn');
    
    if (syncBtn) {
        syncBtn.disabled = true;
        syncBtn.textContent = 'Sincronizando...';
    }
    
    devicesList.innerHTML = '<div class="loading-spinner">Sincronizando dispositivos...</div>';
    
    try {
        const response = await fetchWithAuth(`${API_BASE_URL}/api/Ewelink/sincronizar`, {
            method: 'POST'
        });
        
        if (!response.ok) {
            const error = await response.json();
            throw new Error(error.message || 'Erro ao sincronizar dispositivos');
        }
        
        const data = await response.json();
        showNotification('Dispositivos sincronizados com sucesso!', 'success');
        
        // Recarregar dispositivos
        await loadEwelinkDevices();
    } catch (error) {
        console.error('Erro ao sincronizar dispositivos:', error);
        devicesList.innerHTML = '<div class="error-message show">Erro ao sincronizar: ' + error.message + '</div>';
        showNotification('Erro ao sincronizar dispositivos: ' + error.message, 'error');
    } finally {
        if (syncBtn) {
            syncBtn.disabled = false;
            syncBtn.textContent = 'Sincronizar Dispositivos';
        }
    }
}

// Calcular assinatura HMAC-SHA256 para autorização OAuth
async function calculateOAuthSignature(clientId, seq, clientSecret) {
    // Usar Web Crypto API para calcular HMAC-SHA256
    const message = `${clientId}_${seq}`;
    const encoder = new TextEncoder();
    const keyData = encoder.encode(clientSecret);
    const messageData = encoder.encode(message);
    
    const key = await crypto.subtle.importKey(
        'raw',
        keyData,
        { name: 'HMAC', hash: 'SHA-256' },
        false,
        ['sign']
    );
    
    const signature = await crypto.subtle.sign('HMAC', key, messageData);
    const base64Signature = btoa(String.fromCharCode(...new Uint8Array(signature)));
    
    return base64Signature;
}

// Gerar nonce de 8 caracteres
function generateNonce() {
    const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789';
    let nonce = '';
    for (let i = 0; i < 8; i++) {
        nonce += chars.charAt(Math.floor(Math.random() * chars.length));
    }
    return nonce;
}

// Abrir login Ewelink (redirecionamento direto)
async function openEwelinkLoginModal() {
    try {
        const clientId = 'qPNNDkWlhKwh4xn41bteq2qD02aiGs3D';
        const clientSecret = 'kdG0r5OPddNB90tPKvarWyMWmpppIX9s';
        const seq = Date.now();
        const nonce = generateNonce();
        // IMPORTANTE: O redirectUrl deve ser EXATAMENTE igual ao registrado no console Ewelink
        // O redirectUrl na URL de autorização deve ser o mesmo usado na troca do token
        const redirectUrlRaw = 'https://starkaid.runasp.net/auth/ewelink/callback.html';
        const redirectUrl = encodeURIComponent(redirectUrlRaw);
        const state = 'starkaid';
        const grantType = 'authorization_code';
        
        console.log('Gerando URL de autorização Ewelink:', { 
            redirectUrlRaw, 
            redirectUrl, 
            redirectUrlEncoded: redirectUrl,
            seq, 
            nonce,
            timestamp: new Date().toISOString()
        });
        
        // Calcular assinatura: HMAC-SHA256({clientId}_{seq})
        const authorization = await calculateOAuthSignature(clientId, seq, clientSecret);
        
        // Montar URL conforme documentação
        // IMPORTANTE: redirectUrl aqui é o encoded, mas no backend usamos o raw
        const authUrl = `https://c2ccdn.coolkit.cc/oauth/index.html?clientId=${clientId}&seq=${seq}&authorization=${encodeURIComponent(authorization)}&redirectUrl=${redirectUrl}&grantType=${grantType}&state=${state}&nonce=${nonce}&showQRCode=false`;
        
        console.log('URL de autorização gerada:', authUrl);
        console.log('IMPORTANTE: redirectUrl usado na autorização (encoded):', redirectUrl);
        console.log('IMPORTANTE: redirectUrl que deve ser usado no token (raw):', redirectUrlRaw);
        
        // Redirecionar a página inteira (não popup) para evitar problemas de CORS
        window.location.href = authUrl;
    } catch (error) {
        console.error('Erro ao gerar URL de autorização:', error);
        showNotification('Erro ao gerar URL de autorização: ' + error.message, 'error');
    }
}


// Fazer logout do Ewelink
async function logoutEwelink() {
    if (!confirm('Tem certeza que deseja desconectar sua conta Ewelink?')) {
        return;
    }
    
    try {
        const response = await fetchWithAuth(`${API_BASE_URL}/api/Ewelink/logout`, {
            method: 'POST'
        });
        
        if (!response.ok) {
            const error = await response.json();
            throw new Error(error.message || 'Erro ao fazer logout');
        }
        
        showNotification('Desconectado do Ewelink com sucesso!', 'success');
        await checkEwelinkStatus();
    } catch (error) {
        console.error('Erro ao fazer logout:', error);
        showNotification('Erro ao fazer logout: ' + error.message, 'error');
    }
}

// Função auxiliar para mostrar notificações
function showNotification(message, type = 'info') {
    // Criar elemento de notificação
    const notification = document.createElement('div');
    notification.style.cssText = `
        position: fixed;
        top: 20px;
        right: 20px;
        padding: 1rem 1.5rem;
        background: ${type === 'success' ? '#10b981' : type === 'error' ? '#ef4444' : '#3b82f6'};
        color: white;
        border-radius: 8px;
        box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
        z-index: 10000;
        animation: slideIn 0.3s ease-out;
    `;
    notification.textContent = message;
    
    document.body.appendChild(notification);
    
    // Remover após 3 segundos
    setTimeout(() => {
        notification.style.animation = 'slideOut 0.3s ease-out';
        setTimeout(() => {
            document.body.removeChild(notification);
        }, 300);
    }, 3000);
}

// Adicionar estilos CSS para animações (se não existirem)
if (!document.getElementById('notification-styles')) {
    const style = document.createElement('style');
    style.id = 'notification-styles';
    style.textContent = `
        @keyframes slideIn {
            from {
                transform: translateX(100%);
                opacity: 0;
            }
            to {
                transform: translateX(0);
                opacity: 1;
            }
        }
        @keyframes slideOut {
            from {
                transform: translateX(0);
                opacity: 1;
            }
            to {
                transform: translateX(100%);
                opacity: 0;
            }
        }
        .switch-status.on {
            color: #10b981;
            font-weight: bold;
        }
        .switch-status.off {
            color: #ef4444;
            font-weight: bold;
        }
    `;
    document.head.appendChild(style);
}

// Função para carregar previsão do tempo
async function loadWeatherForecast() {
    const contentDiv = document.getElementById('weather-forecast-content');
    if (!contentDiv) return;

    try {
        contentDiv.innerHTML = '<div class="loading-spinner">Carregando previsão do tempo...</div>';

        const response = await fetchWithAuth(`${API_BASE_URL}/api/weather/forecast`);

        if (!response.ok) {
            let errorMessage = 'Erro ao carregar previsão do tempo';
            try {
                const contentType = response.headers.get('content-type');
                if (contentType && contentType.includes('application/json')) {
                    const error = await response.json();
                    errorMessage = error.message || error.error || errorMessage;
                } else {
                    const errorText = await response.text();
                    errorMessage = errorText || errorMessage;
                }
            } catch (e) {
                // Se não conseguir parsear, usar mensagem padrão baseada no status
                if (response.status === 404) {
                    errorMessage = 'Não foi possível obter a previsão do tempo para sua localização.';
                } else if (response.status === 400) {
                    errorMessage = 'Cidade não cadastrada. Por favor, atualize seu perfil com a cidade.';
                } else if (response.status === 401) {
                    errorMessage = 'Não autorizado. Por favor, faça login novamente.';
                }
            }
            throw new Error(errorMessage);
        }

        const data = await response.json();
        renderWeatherForecast(data);
    } catch (error) {
        console.error('Erro ao carregar previsão do tempo:', error);
        contentDiv.innerHTML = `
            <div style="padding: 2rem; text-align: center; color: var(--error-color);">
                <p>❌ ${error.message || 'Erro ao carregar previsão do tempo'}</p>
                <p style="margin-top: 1rem; font-size: 0.9rem; color: var(--text-secondary);">
                    ${error.message && error.message.includes('cidade') ? '' : 'Certifique-se de ter cadastrado sua cidade no perfil.'}
                </p>
            </div>
        `;
    }
}

function renderWeatherForecast(data) {
    const contentDiv = document.getElementById('weather-forecast-content');
    if (!contentDiv || !data) return;

    let html = '';

    // Tempo atual
    if (data.current) {
        const current = data.current;
        html += `
            <div style="background: linear-gradient(135deg, rgba(0, 180, 255, 0.1) 0%, rgba(0, 120, 200, 0.1) 100%); 
                        padding: 2rem; border-radius: 16px; margin-bottom: 2rem; 
                        border: 1px solid rgba(0, 180, 255, 0.2);">
                <h3 style="margin: 0 0 1.5rem 0; color: var(--text-primary);">🌡️ Tempo Atual</h3>
                <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 1.5rem;">
                    <div style="background: rgba(0, 0, 0, 0.2); padding: 1.5rem; border-radius: 12px;">
                        <div style="font-size: 0.9rem; color: var(--text-secondary); margin-bottom: 0.5rem;">Temperatura</div>
                        <div style="font-size: 2.5rem; font-weight: bold; color: var(--accent-color);">
                            ${Math.round(current.temperature)}°C
                        </div>
                    </div>
                    <div style="background: rgba(0, 0, 0, 0.2); padding: 1.5rem; border-radius: 12px;">
                        <div style="font-size: 0.9rem; color: var(--text-secondary); margin-bottom: 0.5rem;">Condição</div>
                        <div style="font-size: 1.2rem; font-weight: bold; color: var(--text-primary);">
                            ${current.weatherDescription}
                        </div>
                    </div>
                    <div style="background: rgba(0, 0, 0, 0.2); padding: 1.5rem; border-radius: 12px;">
                        <div style="font-size: 0.9rem; color: var(--text-secondary); margin-bottom: 0.5rem;">Vento</div>
                        <div style="font-size: 1.2rem; font-weight: bold; color: var(--text-primary);">
                            ${Math.round(current.windSpeed)} km/h ${current.windDirectionText}
                        </div>
                    </div>
                </div>
            </div>
        `;
    }

    // Previsão horária (próximas 12 horas)
    if (data.hourly && data.hourly.length > 0) {
        html += `
            <div style="margin-bottom: 2rem;">
                <h3 style="margin: 0 0 1rem 0; color: var(--text-primary);">📊 Previsão Horária (Próximas 12h)</h3>
                <div style="display: grid; grid-template-columns: repeat(auto-fill, minmax(120px, 1fr)); gap: 1rem; 
                            overflow-x: auto; padding: 1rem; background: rgba(0, 0, 0, 0.1); border-radius: 12px;">
        `;
        
        data.hourly.slice(0, 12).forEach(hour => {
            const time = new Date(hour.time);
            html += `
                <div style="background: rgba(0, 180, 255, 0.1); padding: 1rem; border-radius: 8px; 
                            text-align: center; border: 1px solid rgba(0, 180, 255, 0.2);">
                    <div style="font-size: 0.8rem; color: var(--text-secondary); margin-bottom: 0.5rem;">
                        ${time.getHours().toString().padStart(2, '0')}:${time.getMinutes().toString().padStart(2, '0')}
                    </div>
                    <div style="font-size: 1.5rem; font-weight: bold; color: var(--accent-color); margin-bottom: 0.5rem;">
                        ${Math.round(hour.temperature)}°
                    </div>
                    <div style="font-size: 0.75rem; color: var(--text-secondary);">
                        ${hour.weatherDescription}
                    </div>
                    ${hour.precipitation > 0 ? `
                        <div style="font-size: 0.7rem; color: #60a5fa; margin-top: 0.5rem;">
                            💧 ${hour.precipitation.toFixed(1)}mm
                        </div>
                    ` : ''}
                </div>
            `;
        });
        
        html += `</div></div>`;
    }

    // Previsão diária (próximos 4 dias)
    if (data.daily && data.daily.length > 0) {
        html += `
            <div>
                <h3 style="margin: 0 0 1rem 0; color: var(--text-primary);">📅 Previsão Diária (Próximos 4 dias)</h3>
                <div style="display: flex; flex-direction: column; gap: 0.75rem;">
        `;
        
        data.daily.slice(0, 4).forEach(day => {
            const date = new Date(day.date);
            const dayName = date.toLocaleDateString('pt-BR', { weekday: 'long', day: 'numeric', month: 'short' });
            html += `
                <div style="background: rgba(0, 0, 0, 0.1); padding: 1.5rem; border-radius: 12px; 
                            display: flex; justify-content: space-between; align-items: center;
                            border: 1px solid rgba(0, 180, 255, 0.1);">
                    <div style="flex: 1;">
                        <div style="font-weight: bold; color: var(--text-primary); margin-bottom: 0.5rem;">
                            ${dayName.charAt(0).toUpperCase() + dayName.slice(1)}
                        </div>
                        <div style="font-size: 0.9rem; color: var(--text-secondary);">
                            ${day.weatherDescription}
                        </div>
                        ${day.precipitation > 0 ? `
                            <div style="font-size: 0.85rem; color: #60a5fa; margin-top: 0.5rem;">
                                💧 ${day.precipitation.toFixed(1)}mm
                            </div>
                        ` : ''}
                    </div>
                    <div style="display: flex; align-items: center; gap: 1rem;">
                        <div style="text-align: right;">
                            <div style="font-size: 1.5rem; font-weight: bold; color: var(--accent-color);">
                                ${Math.round(day.temperatureMax)}°
                            </div>
                            <div style="font-size: 1rem; color: var(--text-secondary);">
                                ${Math.round(day.temperatureMin)}°
                            </div>
                        </div>
                        <div style="text-align: right; font-size: 0.85rem; color: var(--text-secondary);">
                            <div>🌬️ ${Math.round(day.windSpeedMax)} km/h</div>
                        </div>
                    </div>
                </div>
            `;
        });
        
        html += `</div></div>`;
    }

    contentDiv.innerHTML = html || '<p style="text-align: center; color: var(--text-secondary);">Nenhum dado disponível.</p>';
}

// ========== FUNÇÕES DE MANUTENÇÃO ==========

let manutencaoSoftwareUserId = null;
let manutencaoAppUserId = null;
let supportChatConnection = null;

// Software
async function iniciarManutencaoSoftware() {
    const userIdInput = document.getElementById('manutencao-software-userid').value;
    if (!userIdInput) {
        alert('Digite o UserId');
        return;
    }

    // Converter string para Guid se necessário
    let userId = userIdInput;
    // Se não for um Guid válido, tentar buscar do usuário atual via API
    if (!userIdInput.match(/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i)) {
        // Se for email, buscar userId do usuário atual via API
        try {
            const userResponse = await fetchWithAuth(`${API_BASE_URL}/api/Users/me`);
            if (userResponse.ok) {
                const currentUser = await userResponse.json();
                if (currentUser && currentUser.id) {
                    userId = currentUser.id;
                } else {
                    alert('UserId inválido. Use um GUID válido.');
                    return;
                }
            } else {
                alert('UserId inválido. Use um GUID válido.');
                return;
            }
        } catch (error) {
            alert('Erro ao buscar usuário. Use um GUID válido.');
            return;
        }
    }

    try {
        const response = await fetchWithAuth(`${API_BASE_URL}/api/manutencao/software/iniciar`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ userId: userId })
        });

        if (response.ok) {
            const data = await response.json();
            manutencaoSoftwareUserId = userId;
            document.getElementById('manutencao-software-status').style.display = 'block';
            document.getElementById('manutencao-software-status').querySelector('p').textContent = data.message;
        } else {
            alert('Erro ao iniciar manutenção');
        }
    } catch (error) {
        console.error('Erro:', error);
        alert('Erro ao iniciar manutenção');
    }
}

async function finalizarManutencaoSoftware() {
    const userIdInput = document.getElementById('manutencao-software-userid').value || manutencaoSoftwareUserId;
    if (!userIdInput) {
        alert('Digite o UserId ou Email');
        return;
    }

    const userId = await obterUserId(userIdInput);
    if (!userId) {
        alert('Usuário não encontrado. Verifique o UserId ou Email.');
        return;
    }

    try {
        const response = await fetchWithAuth(`${API_BASE_URL}/api/manutencao/software/finalizar`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ userId })
        });

        if (response.ok) {
            const data = await response.json();
            document.getElementById('manutencao-software-status').style.display = 'block';
            document.getElementById('manutencao-software-status').querySelector('p').textContent = data.message;
            manutencaoSoftwareUserId = null;
        } else {
            alert('Erro ao finalizar manutenção');
        }
    } catch (error) {
        console.error('Erro:', error);
        alert('Erro ao finalizar manutenção');
    }
}

async function alterarSenhaSoftware() {
    const userIdInput = document.getElementById('manutencao-software-userid').value || manutencaoSoftwareUserId;
    const novaSenha = document.getElementById('manutencao-software-nova-senha').value;
    
    if (!userIdInput || !novaSenha) {
        alert('Preencha UserId/Email e Nova Senha');
        return;
    }

    const userId = await obterUserId(userIdInput);
    if (!userId) {
        alert('Usuário não encontrado. Verifique o UserId ou Email.');
        return;
    }

    try {
        const response = await fetchWithAuth(`${API_BASE_URL}/api/manutencao/software/alterar-senha`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ userId, novaSenha })
        });

        if (response.ok) {
            alert('Senha alterada com sucesso');
            document.getElementById('manutencao-software-nova-senha').value = '';
        } else {
            alert('Erro ao alterar senha');
        }
    } catch (error) {
        console.error('Erro:', error);
        alert('Erro ao alterar senha');
    }
}

async function salvarNomeAssistenteSoftware() {
    const userIdInput = document.getElementById('manutencao-software-userid').value || manutencaoSoftwareUserId;
    const nomeAssistente = document.getElementById('manutencao-software-nome-assistente').value;
    
    if (!userIdInput || !nomeAssistente) {
        alert('Preencha UserId/Email e Nome do Assistente');
        return;
    }

    const userId = await obterUserId(userIdInput);
    if (!userId) {
        alert('Usuário não encontrado. Verifique o UserId ou Email.');
        return;
    }

    try {
        const response = await fetchWithAuth(`${API_BASE_URL}/api/manutencao/software/salvar-nome-assistente`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ userId, nomeAssistente })
        });

        if (response.ok) {
            alert('Nome do assistente salvo com sucesso');
        } else {
            alert('Erro ao salvar nome do assistente');
        }
    } catch (error) {
        console.error('Erro:', error);
        alert('Erro ao salvar nome do assistente');
    }
}

// Função auxiliar para converter email/string em userId (GUID)
async function obterUserId(userIdInput) {
    if (!userIdInput) {
        return null;
    }
    
    // Se já for um GUID válido, retornar diretamente
    if (userIdInput.match(/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i)) {
        return userIdInput;
    }
    
    // Se for email, buscar userId via API
    try {
        const response = await fetchWithAuth(`${API_BASE_URL}/api/Users/by-email/${encodeURIComponent(userIdInput)}`);
        if (response.ok) {
            const user = await response.json();
            return user?.id || user?.Id || null;
        }
    } catch (error) {
        console.error('Erro ao buscar usuário por email:', error);
    }
    
    return null;
}

async function verDispositivosEspSoftware() {
    const userIdInput = document.getElementById('manutencao-software-userid').value || manutencaoSoftwareUserId;
    if (!userIdInput) {
        alert('Digite o UserId ou Email');
        return;
    }

    const userId = await obterUserId(userIdInput);
    if (!userId) {
        alert('Usuário não encontrado. Verifique o UserId ou Email.');
        return;
    }

    try {
        const response = await fetchWithAuth(`${API_BASE_URL}/api/manutencao/software/dispositivos/${userId}`);
        if (response.ok) {
            const dispositivos = await response.json();
            // Abrir modal com lista de dispositivos
            mostrarModalDispositivosEsp(dispositivos);
        } else {
            alert('Erro ao buscar dispositivos');
        }
    } catch (error) {
        console.error('Erro:', error);
        alert('Erro ao buscar dispositivos');
    }
}

async function verComandosSociaisSoftware() {
    const userIdInput = document.getElementById('manutencao-software-userid').value || manutencaoSoftwareUserId;
    if (!userIdInput) {
        alert('Digite o UserId ou Email');
        return;
    }

    const userId = await obterUserId(userIdInput);
    if (!userId) {
        alert('Usuário não encontrado. Verifique o UserId ou Email.');
        return;
    }

    try {
        const response = await fetchWithAuth(`${API_BASE_URL}/api/manutencao/software/comandos-sociais/${userId}`);
        if (response.ok) {
            const comandos = await response.json();
            // Abrir modal com lista de comandos
            mostrarModalComandosSociais(comandos);
        } else {
            alert('Erro ao buscar comandos sociais');
        }
    } catch (error) {
        console.error('Erro:', error);
        alert('Erro ao buscar comandos sociais');
    }
}

async function carregarUltimosComandosSoftware() {
    const userIdInput = document.getElementById('manutencao-software-userid').value || manutencaoSoftwareUserId;
    if (!userIdInput) {
        alert('Digite o UserId ou Email');
        return;
    }

    const userId = await obterUserId(userIdInput);
    if (!userId) {
        alert('Usuário não encontrado. Verifique o UserId ou Email.');
        return;
    }

    try {
        const response = await fetchWithAuth(`${API_BASE_URL}/api/manutencao/software/ultimos-comandos/${userId}`);
        if (response.ok) {
            const dados = await response.json();
            document.getElementById('ultimo-comando-ia-soft').textContent = dados.ultimoComandoIA || '-';
            document.getElementById('ultima-resposta-ia-soft').textContent = dados.ultimaRespostaIA || '-';
            document.getElementById('ultimo-comando-automacao-soft').textContent = dados.ultimoComandoAutomacao || '-';
            document.getElementById('ultimo-comando-social-soft').textContent = dados.ultimoComandoSocial || '-';
            document.getElementById('ultima-resposta-social-soft').textContent = dados.ultimaRespostaSocial || '-';
        } else {
            alert('Erro ao carregar últimos comandos');
        }
    } catch (error) {
        console.error('Erro:', error);
        alert('Erro ao carregar últimos comandos');
    }
}

async function limparCacheSoftware() {
    const userIdInput = document.getElementById('manutencao-software-userid').value || manutencaoSoftwareUserId;
    if (!userIdInput) {
        alert('Digite o UserId ou Email');
        return;
    }

    const userId = await obterUserId(userIdInput);
    if (!userId) {
        alert('Usuário não encontrado. Verifique o UserId ou Email.');
        return;
    }

    try {
        const response = await fetchWithAuth(`${API_BASE_URL}/api/manutencao/software/limpar-cache`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ userId })
        });

        if (response.ok) {
            alert('Cache limpo com sucesso');
        } else {
            alert('Erro ao limpar cache');
        }
    } catch (error) {
        console.error('Erro:', error);
        alert('Erro ao limpar cache');
    }
}

async function limparDadosSoftware() {
    const userIdInput = document.getElementById('manutencao-software-userid').value || manutencaoSoftwareUserId;
    if (!userIdInput) {
        alert('Digite o UserId ou Email');
        return;
    }

    const userId = await obterUserId(userIdInput);
    if (!userId) {
        alert('Usuário não encontrado. Verifique o UserId ou Email.');
        return;
    }

    if (!confirm('Tem certeza que deseja limpar os dados? Esta ação não pode ser desfeita.')) {
        return;
    }

    try {
        const response = await fetchWithAuth(`${API_BASE_URL}/api/manutencao/software/limpar-dados`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ userId })
        });

        if (response.ok) {
            alert('Dados limpos com sucesso');
        } else {
            alert('Erro ao limpar dados');
        }
    } catch (error) {
        console.error('Erro:', error);
        alert('Erro ao limpar dados');
    }
}

async function logoutSoftware() {
    const userIdInput = document.getElementById('manutencao-software-userid').value || manutencaoSoftwareUserId;
    if (!userIdInput) {
        alert('Digite o UserId ou Email');
        return;
    }

    const userId = await obterUserId(userIdInput);
    if (!userId) {
        alert('Usuário não encontrado. Verifique o UserId ou Email.');
        return;
    }

    if (!confirm('Tem certeza que deseja deslogar o usuário?')) {
        return;
    }

    try {
        const response = await fetchWithAuth(`${API_BASE_URL}/api/manutencao/software/logout`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ userId })
        });

        if (response.ok) {
            alert('Usuário deslogado com sucesso');
        } else {
            alert('Erro ao deslogar usuário');
        }
    } catch (error) {
        console.error('Erro:', error);
        alert('Erro ao deslogar usuário');
    }
}

// App
async function limparCacheApp() {
    const userId = document.getElementById('manutencao-app-userid').value || manutencaoAppUserId;
    if (!userId) {
        alert('Digite o UserId');
        return;
    }

    try {
        const response = await fetchWithAuth(`${API_BASE_URL}/api/manutencao/app/limpar-cache`, {
            method: 'POST',
            body: JSON.stringify({ userId })
        });

        if (response.ok) {
            alert('Cache do app limpo com sucesso');
        } else {
            alert('Erro ao limpar cache');
        }
    } catch (error) {
        console.error('Erro:', error);
        alert('Erro ao limpar cache');
    }
}

async function limparDadosApp() {
    const userId = document.getElementById('manutencao-app-userid').value || manutencaoAppUserId;
    if (!userId) {
        alert('Digite o UserId');
        return;
    }

    if (!confirm('Tem certeza que deseja limpar os dados do app? Esta ação não pode ser desfeita.')) {
        return;
    }

    try {
        const response = await fetchWithAuth(`${API_BASE_URL}/api/manutencao/app/limpar-dados`, {
            method: 'POST',
            body: JSON.stringify({ userId })
        });

        if (response.ok) {
            alert('Dados do app limpos com sucesso');
        } else {
            alert('Erro ao limpar dados');
        }
    } catch (error) {
        console.error('Erro:', error);
        alert('Erro ao limpar dados');
    }
}

async function logoutApp() {
    const userId = document.getElementById('manutencao-app-userid').value || manutencaoAppUserId;
    if (!userId) {
        alert('Digite o UserId');
        return;
    }

    if (!confirm('Tem certeza que deseja deslogar o usuário do app?')) {
        return;
    }

    try {
        const response = await fetchWithAuth(`${API_BASE_URL}/api/manutencao/app/logout`, {
            method: 'POST',
            body: JSON.stringify({ userId })
        });

        if (response.ok) {
            alert('Usuário deslogado do app com sucesso');
        } else {
            alert('Erro ao deslogar usuário');
        }
    } catch (error) {
        console.error('Erro:', error);
        alert('Erro ao deslogar usuário');
    }
}

async function carregarUltimosComandosApp() {
    const userId = document.getElementById('manutencao-app-userid').value || manutencaoAppUserId;
    if (!userId) {
        alert('Digite o UserId');
        return;
    }

    try {
        const response = await fetchWithAuth(`${API_BASE_URL}/api/manutencao/app/ultimos-comandos/${userId}`);
        if (response.ok) {
            const dados = await response.json();
            document.getElementById('ultimo-comando-ia-app').textContent = dados.ultimoComandoIA || '-';
            document.getElementById('ultima-resposta-ia-app').textContent = dados.ultimaRespostaIA || '-';
            document.getElementById('ultimo-comando-automacao-app').textContent = dados.ultimoComandoAutomacao || '-';
        } else {
            alert('Erro ao carregar últimos comandos');
        }
    } catch (error) {
        console.error('Erro:', error);
        alert('Erro ao carregar últimos comandos');
    }
}

// ========== CHAT DE SUPORTE ==========

async function conectarChatSuporte() {
    if (supportChatConnection && supportChatConnection.state === signalR.HubConnectionState.Connected) {
        alert('Já está conectado ao chat');
        return;
    }

    if (!authToken) {
        alert('Você precisa estar logado para usar o chat');
        return;
    }

    try {
        supportChatConnection = new signalR.HubConnectionBuilder()
            .withUrl(`${API_BASE_URL}/hubs/support-chat?origem=software`, {
                accessTokenFactory: () => authToken
            })
            .withAutomaticReconnect()
            .build();

        // Event listeners
        supportChatConnection.on("QueuePosition", (data) => {
            const statusDiv = document.getElementById('chat-queue-status');
            statusDiv.style.display = 'block';
            statusDiv.querySelector('p').textContent = data.message;
        });

        supportChatConnection.on("NextInQueue", (data) => {
            const statusDiv = document.getElementById('chat-queue-status');
            statusDiv.style.display = 'block';
            statusDiv.querySelector('p').textContent = data.message;
        });

        supportChatConnection.on("ReceiveMessage", (message) => {
            adicionarMensagemChat(message.message, message.sender);
        });

        supportChatConnection.on("Error", (error) => {
            const errorDiv = document.createElement('div');
            errorDiv.style.cssText = 'padding: 1rem; background: #ff4444; color: white; border-radius: 8px; margin-bottom: 1rem;';
            errorDiv.textContent = 'Erro: ' + error;
            document.getElementById('chat-messages').appendChild(errorDiv);
        });

        supportChatConnection.on("LimiteAtingido", () => {
            document.getElementById('chat-message-input').disabled = true;
            document.getElementById('chat-message-input').placeholder = 'Limite de contexto atingido. Preencha o formulário abaixo.';
            mostrarFormularioLimite();
        });

        supportChatConnection.onreconnecting(() => {
            const statusDiv = document.getElementById('chat-queue-status');
            statusDiv.style.display = 'block';
            statusDiv.querySelector('p').textContent = 'Reconectando...';
        });

        supportChatConnection.onreconnected(() => {
            const statusDiv = document.getElementById('chat-queue-status');
            statusDiv.style.display = 'block';
            statusDiv.querySelector('p').textContent = 'Reconectado!';
        });

        await supportChatConnection.start();
        console.log('Conectado ao chat de suporte');
    } catch (error) {
        console.error('Erro ao conectar:', error);
        alert('Erro ao conectar ao chat de suporte');
    }
}

async function desconectarChatSuporte() {
    if (supportChatConnection) {
        await supportChatConnection.stop();
        supportChatConnection = null;
        document.getElementById('chat-queue-status').style.display = 'none';
        document.getElementById('chat-messages').innerHTML = '<p style="color: var(--light-text); text-align: center; margin-top: 50%;">Desconectado do chat.</p>';
    }
}

async function enviarMensagemChat() {
    const input = document.getElementById('chat-message-input');
    const message = input.value.trim();

    if (!message) {
        return;
    }

    if (!supportChatConnection || supportChatConnection.state !== signalR.HubConnectionState.Connected) {
        alert('Você precisa estar conectado ao chat');
        return;
    }

    try {
        await supportChatConnection.invoke("SendMessage", message);
        input.value = '';
        adicionarMensagemChat(message, 'user');
    } catch (error) {
        console.error('Erro ao enviar mensagem:', error);
        alert('Erro ao enviar mensagem');
    }
}

function adicionarMensagemChat(mensagem, sender) {
    const messagesDiv = document.getElementById('chat-messages');
    const messageDiv = document.createElement('div');
    messageDiv.style.cssText = 'margin-bottom: 1rem; padding: 0.75rem; border-radius: 8px; ' +
        (sender === 'user' ? 'background: var(--primary-color); color: white; text-align: right; margin-left: 20%;' :
         sender === 'ia' ? 'background: var(--dark-surface); color: var(--light-text); margin-right: 20%;' :
         'background: var(--dark-bg); color: var(--light-text); margin-right: 20%;');
    
    messageDiv.textContent = mensagem;
    messagesDiv.appendChild(messageDiv);
    messagesDiv.scrollTop = messagesDiv.scrollHeight;
}

// Permitir Enter para enviar
document.addEventListener('DOMContentLoaded', () => {
    const chatInput = document.getElementById('chat-message-input');
    if (chatInput) {
        chatInput.addEventListener('keypress', (e) => {
            if (e.key === 'Enter') {
                enviarMensagemChat();
            }
        });
    }
});

// Funções auxiliares para modais
function mostrarModalDispositivosEsp(dispositivos) {
    // Implementar modal similar aos existentes
    alert(`Total de dispositivos: ${dispositivos.length}\n\nUse a funcionalidade de dispositivos ESP na aba principal para gerenciar.`);
}

function mostrarModalComandosSociais(comandos) {
    // Implementar modal similar aos existentes
    alert(`Total de comandos sociais: ${comandos.length}\n\nUse a funcionalidade de comandos sociais na aba principal para gerenciar.`);
}

function mostrarFormularioLimite() {
    document.getElementById('formulario-limite').style.display = 'block';
    document.getElementById('chat-message-input').disabled = true;
}

async function enviarFormularioLimite() {
    const mensagem = document.getElementById('formulario-mensagem').value;
    const detalhes = document.getElementById('formulario-detalhes').value;

    if (!mensagem.trim()) {
        alert('Por favor, descreva o problema');
        return;
    }

    try {
        const response = await fetchWithAuth(`${API_BASE_URL}/api/suporte/enviar-formulario-limite`, {
            method: 'POST',
            body: JSON.stringify({ mensagem, detalhes })
        });

        if (response.ok) {
            const data = await response.json();
            alert(data.message || 'Formulário enviado com sucesso!');
            document.getElementById('formulario-limite').style.display = 'none';
            document.getElementById('formulario-mensagem').value = '';
            document.getElementById('formulario-detalhes').value = '';
        } else {
            alert('Erro ao enviar formulário');
        }
    } catch (error) {
        console.error('Erro:', error);
        alert('Erro ao enviar formulário');
    }
}

