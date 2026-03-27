import { createRouter, createWebHistory } from 'vue-router'
import { useUserStore } from '../store/user'

const routes = [
	{
		path:'/login',
		name:'Login',
		component:() => import('../views/login/Index.vue')
    },
	{
		path:'/',
		component: () => import('../layout/MainLayout.vue'),
		redirect:'/',
		children:[
			// 首页
			{
				path:'/',
				name:'Home',
				meta: { title: '首页' },
				component: () => import('../views/home/Index.vue')
			},
			// 审批中心
			{
				path:'approval/submit',
				name:'ApprovalSubmit',
				meta: { title: '发起申请' },
				component: () => import('../views/workflow/Apply.vue')
			},
			{
				path:'approval/pending',
				name:'ApprovalPending',
				meta: { title: '待我审批' },
				component: () => import('../views/workflow/Workbench.vue')
			},
			{
				path:'approval/my-applications',
				name:'MyApplications',
				meta: { title: '我的申请' },
				component: () => import('../views/workflow/MyApplications.vue')
			},
			{
				path:'approval/done',
				name:'ApprovalDone',
				meta: { title: '已办事项' },
				component: () => import('../views/workflow/Done.vue')
			},
			// 工作流管理
			{
				path:'workflow',
				name:'Workflow',
				redirect:'/workflow/workbench',
				meta: { title: '工作流管理' },
				children:[
					{
						path:'workbench',
						name:'WorkflowWorkbench',
						meta: { title: '待我审批' },
						component: () => import('../views/workflow/Workbench.vue')
					},
					{
						path:'done',
						name:'WorkflowDone',
						meta: { title: '已办事项' },
						component: () => import('../views/workflow/Done.vue')
					},
					{
						path:'apply',
						name:'WorkflowApply',
						meta: { title: '发起申请' },
						component: () => import('../views/workflow/Apply.vue')
					},
					{
						path:'my',
						name:'WorkflowMy',
						meta: { title: '我的申请' },
						component: () => import('../views/workflow/MyApplications.vue')
					},
					{
						path:'detail',
						name:'WorkflowDetail',
						meta: { title: '工作流详情' },
						component: () => import('../views/workflow/Detail.vue')
					}
				]
			},
			// 任务管理
			{
				path:'tasks',
				name:'TaskList',
				meta: { title: '任务管理' },
				component: () => import('../views/task/Index.vue')
			},
			// 组织架构
			{
				path:'departments',
				name:'DepartmentList',
				meta: { title: '部门管理' },
				component: () => import('../views/department/Index.vue')
			},
			{
				path:'users',
				name:'UserList',
				meta: { title: '用户管理' },
				component: () => import('../views/user/Index.vue')
			},
			// 通知中心
			{
				path:'notification/unread',
				name:'UnreadNotifications',
				meta: { title: '未读消息' },
				component: () => import('../views/notification/Unread.vue')
			},
			{
				path:'notification/history',
				name:'NotificationHistory',
				meta: { title: '历史消息' },
				component: () => import('../views/notification/History.vue')
			},
			{
				path:'profile',
				name:'Profile',
				meta: { title: '个人档案' },
				component: () => import('../views/profile/Index.vue')
			},
			// 数据看板
			{
				path:'statistics/task',
				name:'TaskStatistics',
				meta: { title: '任务统计' },
				component: () => import('../views/statistics/TaskStatistics.vue')
			}
		]
	}
]

//创建router实例
const router = createRouter({
	history: createWebHistory(),
	routes
})

router.beforeEach((to,from,next) => {
	const userStore = useUserStore()
	const publicPages = ['/login'] //白名单
	const authRequired = !publicPages.includes(to.path)
	const token = userStore.token
	
	if(authRequired && !userStore.token){
		return next('/login')
	}
	
	if(to.path == '/login'){
		if(token) return next('/')
		return next()
	}
	
	if(!token){
		return next('/login')
	}
	
	next()
})

export default router