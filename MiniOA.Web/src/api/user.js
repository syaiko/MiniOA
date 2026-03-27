import request from '../utils/request.js'

// 获取用户列表
export const getUsers = (departmentId = null, search = null) => {
  const params = {}
  if (departmentId) params.departmentId = departmentId
  if (search && search.trim()) params.search = search.trim()
  return request.get('/user', { params })
}

// 获取用户详情
export const getUser = (id) => {
  return request.get(`/user/${id}`)
}

// 获取当前用户信息
export const getCurrentUser = () => {
  return request.get('/user/current')
}

// 创建用户
export const createUser = (data) => {
  return request.post('/user', data)
}

// 更新用户
export const updateUser = (id, data) => {
  return request.put(`/user/${id}`, data)
}

// 删除用户
export const deleteUser = (id) => {
  return request.delete(`/user/${id}`)
}

// 重置密码（管理员操作）
export const resetPassword = (id, newPassword) => {
  return request.post(`/user/${id}/reset-password`, { newPassword })
}

// ==================== 个人档案管理 ====================

// 获取个人资料
export const getProfile = () => {
  return request.get('/user/profile')
}

// 更新个人资料
export const updateProfile = (data) => {
  return request.put('/user/profile', data)
}

// 修改密码
export const changePassword = (currentPassword, newPassword) => {
  return request.put('/user/change-password', { currentPassword, newPassword })
}
