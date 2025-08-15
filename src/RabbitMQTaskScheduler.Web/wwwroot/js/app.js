// Task Scheduler Web Application
class TaskSchedulerApp {
    constructor() {
        this.currentPage = 1;
        this.pageSize = 20;
        this.connection = null;
        this.initializeSignalR();
        this.bindEvents();
        this.loadInitialData();
    }

    // Initialize SignalR connection
    async initializeSignalR() {
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl("/hubs/taskmonitoring")
            .build();

        // Handle real-time updates
        this.connection.on("TaskUpdated", (task) => {
            this.handleTaskUpdate(task);
        });

        this.connection.on("StatisticsUpdated", (statistics) => {
            this.updateStatistics(statistics);
        });

        this.connection.on("ConsumersUpdated", (consumers) => {
            this.updateConsumers(consumers);
        });

        try {
            await this.connection.start();
            console.log("SignalR Connected");
            this.updateConnectionStatus(true);
        } catch (err) {
            console.error("SignalR Connection Error: ", err);
            this.updateConnectionStatus(false);
            // Retry connection after 5 seconds
            setTimeout(() => this.initializeSignalR(), 5000);
        }

        this.connection.onclose(() => {
            console.log("SignalR Disconnected");
            this.updateConnectionStatus(false);
            // Retry connection after 5 seconds
            setTimeout(() => this.initializeSignalR(), 5000);
        });
    }

    updateConnectionStatus(connected) {
        const statusElement = document.getElementById('connectionStatus');
        if (connected) {
            statusElement.textContent = 'Connected';
            statusElement.className = 'badge bg-success me-2 connected';
        } else {
            statusElement.textContent = 'Disconnected';
            statusElement.className = 'badge bg-danger me-2 disconnected';
        }
    }

    // Bind UI events
    bindEvents() {
        // Task type change handler
        const taskTypeSelect = document.getElementById('taskType');
        if (taskTypeSelect) {
            taskTypeSelect.addEventListener('change', this.handleTaskTypeChange.bind(this));
        }

        // Form submission
        const createTaskForm = document.getElementById('createTaskForm');
        if (createTaskForm) {
            createTaskForm.addEventListener('submit', this.handleCreateTask.bind(this));
        }

        // Search functionality
        const searchInput = document.getElementById('taskSearch');
        if (searchInput) {
            searchInput.addEventListener('input', this.debounce(() => this.loadTasks(), 500));
        }

        // Filter change
        const statusFilter = document.getElementById('taskStatusFilter');
        if (statusFilter) {
            statusFilter.addEventListener('change', () => this.loadTasks());
        }
    }

    // Load initial data
    async loadInitialData() {
        await Promise.all([
            this.loadStatistics(),
            this.loadTasks(),
            this.loadConsumers()
        ]);
        
        // Set up periodic updates
        setInterval(() => this.loadStatistics(), 30000); // Every 30 seconds
        setInterval(() => this.loadConsumers(), 10000);  // Every 10 seconds
    }

    // API calls
    async apiCall(url, options = {}) {
        try {
            const response = await fetch(url, {
                headers: {
                    'Content-Type': 'application/json',
                    ...options.headers
                },
                ...options
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            return await response.json();
        } catch (error) {
            console.error('API Error:', error);
            this.showError(`Error: ${error.message}`);
            throw error;
        }
    }

    // Load statistics
    async loadStatistics() {
        try {
            const stats = await this.apiCall('/api/tasks/statistics');
            this.updateStatistics(stats);
        } catch (error) {
            console.error('Error loading statistics:', error);
        }
    }

    // Update statistics display
    updateStatistics(stats) {
        document.getElementById('totalTasks').textContent = stats.totalTasks || 0;
        document.getElementById('pendingTasks').textContent = stats.pendingTasks || 0;
        document.getElementById('completedTasks').textContent = stats.completedTasks || 0;
        document.getElementById('failedTasks').textContent = stats.failedTasks || 0;
        document.getElementById('activeConsumers').textContent = stats.activeConsumers || 0;
        document.getElementById('successRate').textContent = `${(stats.successRate || 0).toFixed(1)}%`;
        document.getElementById('averageExecutionTime').textContent = `${(stats.averageExecutionTime || 0).toFixed(0)}ms`;
    }

    // Load tasks
    async loadTasks() {
        try {
            const search = document.getElementById('taskSearch')?.value || '';
            const status = document.getElementById('taskStatusFilter')?.value || '';
            
            let url = `/api/tasks?page=${this.currentPage}&pageSize=${this.pageSize}`;
            if (search) url += `&search=${encodeURIComponent(search)}`;
            if (status) url += `&status=${status}`;

            const tasks = await this.apiCall(url);
            this.displayTasks(tasks);
        } catch (error) {
            console.error('Error loading tasks:', error);
        }
    }

    // Display tasks in table
    displayTasks(tasks) {
        const tbody = document.getElementById('tasksTableBody');
        if (!tbody) return;

        tbody.innerHTML = '';
        
        if (!tasks || tasks.length === 0) {
            tbody.innerHTML = '<tr><td colspan="7" class="text-center text-muted">No tasks found</td></tr>';
            return;
        }

        tasks.forEach(task => {
            const row = this.createTaskRow(task);
            tbody.appendChild(row);
        });
    }

    // Create task table row
    createTaskRow(task) {
        const row = document.createElement('tr');
        row.id = `task-${task.id}`;
        
        const statusClass = this.getStatusClass(task.status);
        const priorityClass = this.getPriorityClass(task.priority);
        
        row.innerHTML = `
            <td><code>${task.id.substring(0, 8)}</code></td>
            <td>${this.escapeHtml(task.name)}</td>
            <td>${this.getTaskTypeName(task.type)}</td>
            <td><span class="${priorityClass}">${this.getPriorityName(task.priority)}</span></td>
            <td><span class="badge ${statusClass}">${this.getStatusName(task.status)}</span></td>
            <td>${this.formatDate(task.createdAt)}</td>
            <td>
                <button class="btn btn-sm btn-outline-primary" onclick="app.viewTaskDetails('${task.id}')">
                    <i class="fas fa-eye"></i>
                </button>
                <button class="btn btn-sm btn-outline-danger" onclick="app.deleteTask('${task.id}')" 
                        ${task.status === 1 ? 'disabled' : ''}>
                    <i class="fas fa-trash"></i>
                </button>
            </td>
        `;
        
        return row;
    }

    // Load consumers
    async loadConsumers() {
        try {
            const consumers = await this.apiCall('/api/consumers');
            this.updateConsumers(consumers);
        } catch (error) {
            console.error('Error loading consumers:', error);
        }
    }

    // Update consumers display
    updateConsumers(consumers) {
        const tbody = document.getElementById('consumersTableBody');
        if (!tbody) return;

        tbody.innerHTML = '';
        
        if (!consumers || consumers.length === 0) {
            tbody.innerHTML = '<tr><td colspan="9" class="text-center text-muted">No active consumers</td></tr>';
            return;
        }

        consumers.forEach(consumer => {
            const row = document.createElement('tr');
            row.innerHTML = `
                <td><code>${consumer.id.substring(0, 8)}</code></td>
                <td>${this.escapeHtml(consumer.name)}</td>
                <td><span class="badge ${consumer.isActive ? 'bg-success' : 'bg-danger'}">${consumer.isActive ? 'Online' : 'Offline'}</span></td>
                <td>${this.escapeHtml(consumer.machineName)}</td>
                <td>${this.formatDate(consumer.startedAt)}</td>
                <td>${consumer.processedTasksCount}</td>
                <td>${consumer.failedTasksCount}</td>
                <td>${this.formatDate(consumer.lastHeartbeat)}</td>
                <td>${consumer.currentTaskId ? `<code>${consumer.currentTaskId.substring(0, 8)}</code>` : '-'}</td>
            `;
            tbody.appendChild(row);
        });
    }

    // Handle real-time task updates
    handleTaskUpdate(task) {
        const row = document.getElementById(`task-${task.id}`);
        if (row) {
            // Update existing row
            const newRow = this.createTaskRow(task);
            newRow.classList.add('updated');
            row.replaceWith(newRow);
            
            // Remove highlight after animation
            setTimeout(() => {
                const updatedRow = document.getElementById(`task-${task.id}`);
                if (updatedRow) updatedRow.classList.remove('updated');
            }, 1000);
        } else {
            // Add new row if on first page
            if (this.currentPage === 1) {
                this.loadTasks();
            }
        }
        
        // Update statistics
        this.loadStatistics();
    }

    // Show section
    showSection(sectionName) {
        // Hide all sections
        document.querySelectorAll('.section').forEach(section => {
            section.style.display = 'none';
        });
        
        // Show selected section
        const section = document.getElementById(sectionName);
        if (section) {
            section.style.display = 'block';
        }
        
        // Update navbar
        document.querySelectorAll('.nav-link').forEach(link => {
            link.classList.remove('active');
        });
        document.querySelector(`[href="#${sectionName}"]`)?.classList.add('active');
        
        // Load section data
        switch (sectionName) {
            case 'tasks':
                this.loadTasks();
                break;
            case 'consumers':
                this.loadConsumers();
                break;
            case 'dashboard':
                this.loadStatistics();
                break;
        }
    }

    // Handle task type change in form
    handleTaskTypeChange(event) {
        const taskType = parseInt(event.target.value);
        const scheduledGroup = document.getElementById('scheduledAtGroup');
        const delayGroup = document.getElementById('delayTimeGroup');
        
        // Hide all specific fields
        scheduledGroup.style.display = 'none';
        delayGroup.style.display = 'none';
        
        // Show relevant fields
        if (taskType === 1) { // Scheduled
            scheduledGroup.style.display = 'block';
        } else if (taskType === 2) { // Delayed
            delayGroup.style.display = 'block';
        }
    }

    // Handle create task form submission
    async handleCreateTask(event) {
        event.preventDefault();
        
        try {
            const formData = new FormData(event.target);
            const taskType = parseInt(document.getElementById('taskType').value);
            
            const taskData = {
                name: document.getElementById('taskName').value,
                description: document.getElementById('taskDescription').value,
                type: taskType,
                priority: parseInt(document.getElementById('taskPriority').value),
                payload: document.getElementById('taskPayload').value || '{}',
                maxRetries: 3
            };
            
            // Add type-specific fields
            if (taskType === 1) { // Scheduled
                const scheduledAt = document.getElementById('scheduledAt').value;
                if (scheduledAt) {
                    taskData.scheduledAt = new Date(scheduledAt).toISOString();
                }
            } else if (taskType === 2) { // Delayed
                const delayTime = document.getElementById('delayTime').value;
                if (delayTime) {
                    taskData.delayTime = `${delayTime}.00:00:00`; // TimeSpan format
                }
            }
            
            await this.apiCall('/api/tasks', {
                method: 'POST',
                body: JSON.stringify(taskData)
            });
            
            // Close modal and reset form
            const modal = bootstrap.Modal.getInstance(document.getElementById('createTaskModal'));
            modal.hide();
            document.getElementById('createTaskForm').reset();
            
            this.showSuccess('Task created successfully!');
            this.loadTasks();
            this.loadStatistics();
            
        } catch (error) {
            console.error('Error creating task:', error);
            this.showError('Failed to create task');
        }
    }

    // Delete task
    async deleteTask(taskId) {
        if (!confirm('Are you sure you want to delete this task?')) {
            return;
        }
        
        try {
            await this.apiCall(`/api/tasks/${taskId}`, { method: 'DELETE' });
            this.showSuccess('Task deleted successfully!');
            this.loadTasks();
            this.loadStatistics();
        } catch (error) {
            console.error('Error deleting task:', error);
            this.showError('Failed to delete task');
        }
    }

    // View task details
    async viewTaskDetails(taskId) {
        try {
            const task = await this.apiCall(`/api/tasks/${taskId}`);
            this.showTaskDetailsModal(task);
        } catch (error) {
            console.error('Error loading task details:', error);
            this.showError('Failed to load task details');
        }
    }

    // Show task details modal
    showTaskDetailsModal(task) {
        // This would show a modal with task details
        // For now, just show an alert
        alert(`Task Details:\nID: ${task.id}\nName: ${task.name}\nStatus: ${this.getStatusName(task.status)}\nCreated: ${this.formatDate(task.createdAt)}`);
    }

    // Scale consumers
    async scaleConsumers(direction) {
        try {
            const currentCount = await this.apiCall('/api/consumers/count');
            const targetCount = direction === 'up' ? currentCount + 1 : Math.max(1, currentCount - 1);
            
            await this.apiCall('/api/consumers/scale', {
                method: 'POST',
                body: JSON.stringify({ targetCount })
            });
            
            this.showSuccess(`Consumer scaling request sent (target: ${targetCount})`);
            setTimeout(() => this.loadConsumers(), 2000);
            
        } catch (error) {
            console.error('Error scaling consumers:', error);
            this.showError('Failed to scale consumers');
        }
    }

    // Utility functions
    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    formatDate(dateString) {
        if (!dateString) return '-';
        return new Date(dateString).toLocaleString();
    }

    getStatusName(status) {
        const names = ['Pending', 'Processing', 'Completed', 'Failed', 'Cancelled', 'Retrying'];
        return names[status] || 'Unknown';
    }

    getStatusClass(status) {
        const classes = ['status-pending', 'status-processing', 'status-completed', 'status-failed', 'status-cancelled', 'status-retrying'];
        return `badge ${classes[status] || ''}`;
    }

    getPriorityName(priority) {
        const names = ['Low', 'Normal', 'High', 'Critical'];
        return names[priority] || 'Unknown';
    }

    getPriorityClass(priority) {
        const classes = ['priority-low', 'priority-normal', 'priority-high', 'priority-critical'];
        return classes[priority] || '';
    }

    getTaskTypeName(type) {
        const names = ['Immediate', 'Scheduled', 'Delayed', 'Recurring'];
        return names[type] || 'Unknown';
    }

    showError(message) {
        // You could implement a toast notification here
        console.error(message);
    }

    showSuccess(message) {
        // You could implement a toast notification here
        console.log(message);
    }

    debounce(func, wait) {
        let timeout;
        return function executedFunction(...args) {
            const later = () => {
                clearTimeout(timeout);
                func(...args);
            };
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
        };
    }
}

// Global functions for HTML onclick handlers
function showSection(sectionName) {
    app.showSection(sectionName);
}

function showCreateTaskModal() {
    const modal = new bootstrap.Modal(document.getElementById('createTaskModal'));
    modal.show();
}

function showConsumerManager() {
    showSection('consumers');
}

function scaleConsumers(direction) {
    app.scaleConsumers(direction);
}

function loadTasks() {
    app.loadTasks();
}

// Initialize app when DOM is loaded
let app;
document.addEventListener('DOMContentLoaded', () => {
    app = new TaskSchedulerApp();
});