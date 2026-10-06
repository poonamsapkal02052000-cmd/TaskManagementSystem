import api from './client';

// Removes empty values so they are not sent as query parameters.
const clean = (params = {}) =>
  Object.fromEntries(Object.entries(params).filter(([, v]) => v !== '' && v !== null && v !== undefined));

export const authApi = {
  login: (email, password) => api.post('/auth/login', { email, password }).then((r) => r.data),
  register: (fullName, email, password) => api.post('/auth/register', { fullName, email, password }).then((r) => r.data),
  me: () => api.get('/auth/me').then((r) => r.data)
};

export const tasksApi = {
  list: (params) => api.get('/tasks', { params: clean(params) }).then((r) => r.data),
  get: (id) => api.get(`/tasks/${id}`).then((r) => r.data),
  create: (data) => api.post('/tasks', data).then((r) => r.data),
  update: (id, data) => api.put(`/tasks/${id}`, data).then((r) => r.data),
  assign: (id, assignedToId) => api.patch(`/tasks/${id}/assign`, { assignedToId }).then((r) => r.data),
  updateStatus: (id, status) => api.patch(`/tasks/${id}/status`, { status }).then((r) => r.data),
  remove: (id) => api.delete(`/tasks/${id}`),
  comments: (id) => api.get(`/tasks/${id}/comments`).then((r) => r.data),
  addComment: (id, content) => api.post(`/tasks/${id}/comments`, { content }).then((r) => r.data),
  deleteComment: (id, commentId) => api.delete(`/tasks/${id}/comments/${commentId}`)
};

export const teamsApi = {
  list: () => api.get('/teams').then((r) => r.data),
  create: (data) => api.post('/teams', data).then((r) => r.data),
  update: (id, data) => api.put(`/teams/${id}`, data).then((r) => r.data),
  remove: (id) => api.delete(`/teams/${id}`),
  addMember: (id, userId) => api.post(`/teams/${id}/members`, { userId }).then((r) => r.data),
  removeMember: (id, userId) => api.delete(`/teams/${id}/members/${userId}`).then((r) => r.data)
};

export const usersApi = {
  list: (params) => api.get('/users', { params: clean(params) }).then((r) => r.data),
  create: (data) => api.post('/users', data).then((r) => r.data),
  updateRole: (id, role) => api.put(`/users/${id}/role`, { role }).then((r) => r.data),
  remove: (id) => api.delete(`/users/${id}`)
};

export const notificationsApi = {
  list: () => api.get('/notifications').then((r) => r.data),
  unreadCount: () => api.get('/notifications/unread-count').then((r) => r.data.count),
  markRead: (id) => api.patch(`/notifications/${id}/read`),
  markAllRead: () => api.patch('/notifications/read-all')
};

export const dashboardApi = {
  get: (params) => api.get('/dashboard', { params: clean(params) }).then((r) => r.data)
};
