import request from '../utils/request.js'

// 提交流程
export const submitWorkflow = (data) => {
  return request.post('/workflow/submit', data)
}

// 重新提交流程（已退回的流程）
export const resubmitWorkflow = (id, data) => {
  return request.post(`/workflow/${id}/resubmit`, data)
}

// 审批流程
export const approveWorkflow = (data) => {
  return request.post('/workflow/approve', data)
}

// 获取流程详情
export const getWorkflowInstance = (id) => {
  return request.get(`/workflow/${id}`)
}

// 获取我的申请列表
export const getMyInstances = () => {
  return request.get('/workflow/my')
}

// 获取我的待办
export const getMyTodo = () => {
  return request.get('/workflow/todo')
}

// 获取我的已办
export const getMyDone = () => {
  return request.get('/workflow/done')
}

// 获取流程节点列表
export const getWorkflowNodes = (workflowType) => {
  return request.get(`/workflow/nodes/${workflowType}`)
}
