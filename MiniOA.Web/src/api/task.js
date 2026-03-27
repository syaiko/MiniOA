import request from '../utils/request.js'

// 获取我的任务列表
export const getMyTasks = () => {
  return request.get('/task/getMyTasks')
}

// 获取任务详情
export const getTask = (id) => {
  return request.get(`/task/${id}`)
}

// 创建任务
export const createTask = (data) => {
  return request.post('/task/createTask', data)
}

// 更新任务
export const updateTask = (id, data) => {
  return request.put(`/task/${id}`, data)
}

// 删除任务
export const deleteTask = (id) => {
  return request.delete(`/task/${id}`)
}

// 开始处理任务
export const startProcessing = (id) => {
  return request.post(`/task/${id}/start-processing`)
}

// 提交审核
export const submitReview = (id) => {
  return request.post(`/task/${id}/submit-review`)
}

// 审批任务
export const approveTask = (id, isPass) => {
  return request.post(`/task/${id}/approve?isPass=${isPass}`)
}

// 获取任务审计日志
export const getTaskAuditLogs = (id) => {
  return request.get(`/task/${id}/audit-logs`)
}

// 获取任务统计报表（管理员）
export const getTaskStatistics = () => {
  return request.get('/task/statistics')
}

// 获取任务趋势数据（管理员）
export const getTaskTrend = (months = 12) => {
  return request.get(`/task/trend?months=${months}`)
}

// 获取首页仪表盘数据
export const getDashboard = () => {
  return request.get('/task/dashboard')
}
