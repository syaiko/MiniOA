import request from '../utils/request.js'

// 获取部门树
export const getDepartmentTree = () => {
  return request.get('/department/tree')
}

// 创建部门
export const createDepartment = (data) => {
  return request.post('/department', data)
}

// 更新部门
export const updateDepartment = (id, data) => {
  return request.put(`/department/${id}`, data)
}

// 删除部门
export const deleteDepartment = (id) => {
  return request.delete(`/department/${id}`)
}

// 获取部门用户
export const getDepartmentUsers = (id) => {
  return request.get(`/department/${id}/users`)
}

// 获取子部门
export const getSubDepartments = (parentId) => {
  return request.get(`/department/${parentId}/children`)
}
