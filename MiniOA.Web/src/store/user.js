import { defineStore } from 'pinia'
import request from '../utils/request.js'

export const useUserStore = defineStore('user',{
	state: () => ({
		token:'',
		userInfo:null
	}),
	getters: {
		// 检查是否为超级管理员
		isSupAdmin: (state) => {
			return state.userInfo?.role === 0 // SuperAdmin(0) 或 Admin(1)
		},
		// 检查是否为管理员
		isAdmin: (state) => {
			return state.userInfo?.role <= 1 // SuperAdmin(0) 或 Admin(1)
		},
		// 检查是否为部门经理及以上
		isDepartmentManager: (state) => {
			return state.userInfo?.role <= 2 // SuperAdmin(0), Admin(1), DepartmentManager(2)
		},
		// 检查是否为组长及以上
		isTeamLeader: (state) => {
			return state.userInfo?.role <= 3 // SuperAdmin(0), Admin(1), DepartmentManager(2), TeamLeader(3)
		}
	},
	actions: {
		//封装登录逻辑
		async login(loginForm){
			try{
				const res = await request.post('/auth/login',loginForm)
				
				// 此时 res 是拦截器返回的完整 ApiResult 对象
				if(res && res.success){
					const data = res.data
					this.token = data.token
					this.userInfo = {
						id: data.id,
						username: data.username,
						fullName: data.fullName,
						role: data.role,
						roleDisplayName: data.roleDisplayName,
						departmentId: data.departmentId,
						departmentName: data.departmentName
					}
					// 将用户信息存入 localStorage 以便持久化（持久化插件通常需要手动处理或配合配置）
					localStorage.setItem('user', JSON.stringify({
						token: this.token,
						...this.userInfo
					}))
					return true
				}
				return false
			}catch(error){
				console.error('登录失败:', error)
				return false
			}
		},
		
		//退出登录
		logout(){
			this.token = ''
			this.userInfo = null
			localStorage.removeItem('user')  //清理缓存
			location.reload() //刷新页面清除内存状态
		}
	},
	
	persist: true  //开启持久化 
})