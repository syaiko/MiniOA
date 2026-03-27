import axios from 'axios'
import { ElMessage } from 'element-plus'

const service = axios.create({
	baseURL: 'https://familysys.hejiancheng.xyz/api',
	timeout: 10000 //超时时间
})

const handleUnauthorized = async () => {
	// 延迟导入避免循环依赖
	const { useUserStore } = await import('../store/user')
	const router = (await import('../router')).default
	
	const userStore = useUserStore()
	userStore.token = ''
	userStore.userInfo = null
	localStorage.clear()  //清除持久化缓存
	router.replace('/login')  //replace防止后退返回原页面
}

//请求拦截自动注入token
service.interceptors.request.use(
	(config) => {
		// 直接从 localStorage 读取 token，避免循环依赖
		const userStr = localStorage.getItem('user')
		if (userStr) {
			try {
				const user = JSON.parse(userStr)
				if (user.token) {
					config.headers['Authorization'] = `Bearer ${user.token}`
				}
			} catch (e) {
				console.error('解析用户信息失败', e)
			}
		}
		return config
	},
	(error) => Promise.reject(error)
)

//响应拦截器：自动处理后端返回的ApiResult
service.interceptors.response.use(
	(response) => {
		const res = response.data
		const {config} = response

		if (res.code !== 200) {
			// 401 特殊处理，不显示后端消息
			if (res.code === 401) {
				if (!config.url.includes('/auth/login')) {
					ElMessage.error('身份认证过期，请重新登录')
					handleUnauthorized()
				} else {
					ElMessage.error('用户名或密码错误!')
				}
			} else {
				// 其他错误显示后端消息
				ElMessage.error(res.message || '系统开小差了')
			}

			return Promise.reject(new Error(res.message || 'Error'))
		}
		// 返回完整的 ApiResult 对象 (包含 success, message, data, code)
		return res
	},
	(error) => {
		const { response,config } = error
		
		if (response) {
			const status = response.status
			
			switch (status) {
				case 401:
					if (!config.url.toLowerCase().includes('/auth/login')) {
						ElMessage.error('身份认证过期，请重新登录')
						handleUnauthorized()
						
					} else {
						ElMessage.error('用户名或密码错误')
					}
					break
				case 403:
					ElMessage.error('请求失败，权限不足！')
					break
				case 500:
					ElMessage.error('服务器内部错误')
					break
				default:
					// 对于4xx错误，尝试显示后端返回的具体错误信息
					if (status >= 400 && status < 500) {
						const errorData = response.data
						if (errorData?.Message) {
							// ApiResult格式的错误信息
							ElMessage.error(errorData.Message)
						} else if (errorData?.message) {
							// 小写message的错误信息
							ElMessage.error(errorData.message)
						} else if (errorData?.errors) {
							// 标准验证错误格式
							const errors = errorData.errors
							const errorMessages = Object.values(errors).flat()
							ElMessage.error(errorMessages.join('; '))
						} else {
							ElMessage.error('请求失败')
						}
					} else {
						ElMessage.error('网络连接异常')
					}
			}
		} else {
			// 网络错误或超时
			ElMessage.error('网络连接异常')
		}

		return Promise.reject(error)
	}
)

export default service