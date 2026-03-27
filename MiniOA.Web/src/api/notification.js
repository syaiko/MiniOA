import request from '../utils/request'

// 获取未读通知数量
export const getUnreadCount = () => {
  return request.get('/notification/unread-count')
}

// 获取通知列表
export const getNotifications = (params = {}) => {
  return request.get('/notification', { params })
}

// 标记通知为已读
export const markAsRead = (id) => {
  return request.put(`/notification/${id}/read`)
}

// 标记所有通知为已读
export const markAllAsRead = () => {
  return request.put('/notification/read-all')
}
