<template>
	<el-container class="layout-container">
		<el-aside width="200px">
			<div class="logo">Mini-OA 管理系统</div>
			<el-menu :default-active="activeMenu"
			  class="el-menu-vertical" router
			  background-color="#304156" text-color="#bfcbd9"
			  active-text-color="#409EFF">
				<!-- 首页 -->
				<el-menu-item index="/">
					<el-icon><HomeFilled /></el-icon>
					<span>首页</span>
				</el-menu-item>

				<!-- 审批中心 -->
				<el-sub-menu index="approval">
					<template #title>
						<el-icon><Document /></el-icon>
						<span>审批中心</span>
					</template>
					<el-menu-item index="/approval/submit">发起申请</el-menu-item>
					<el-menu-item index="/approval/pending">待我审批</el-menu-item>
					<el-menu-item index="/approval/my-applications">我的申请</el-menu-item>
					<el-menu-item index="/approval/done">已办事项</el-menu-item>
				</el-sub-menu>

				<!-- 任务管理 -->
				<el-menu-item index="/tasks">
					<el-icon><List /></el-icon>
					<span>任务管理</span>
				</el-menu-item>

				<!-- 数据看板 -->
				<el-menu-item index="/statistics/task">
					<el-icon><DataAnalysis /></el-icon>
					<span>数据看板</span>
				</el-menu-item>

				<!-- 组织架构 -->
				<el-sub-menu index="organization">
					<template #title>
						<el-icon><OfficeBuilding /></el-icon>
						<span>组织架构</span>
					</template>
					<el-menu-item index="/departments">部门管理</el-menu-item>
					<el-menu-item index="/users">用户管理</el-menu-item>
				</el-sub-menu>

				<!-- 通知中心 -->
				<el-sub-menu index="notification">
					<template #title>
						<el-icon><Bell /></el-icon>
						<span>通知中心</span>
					</template>
					<el-menu-item index="/notification/unread">
						未读消息
						<el-badge :value="unreadCount" :max="99" :hidden="unreadCount === 0" class="menu-badge" />
					</el-menu-item>
					<el-menu-item index="/notification/history">历史消息</el-menu-item>
				</el-sub-menu>
			</el-menu>
		</el-aside>
		
		<el-container>
			<el-header class="header">
				<div class="header-left">
					<el-breadcrumb separator="/">
						<el-breadcrumb-item :to="{path:'/'}">首页</el-breadcrumb-item>
						<el-breadcrumb-item>{{currentRouteName}}</el-breadcrumb-item>
					</el-breadcrumb>
				</div>
				
				<div class="header-right">
					<NotificationBell />
					<el-dropdown @command="handleCommand">
						<span class="el-dropdown-link">
							<el-avatar :size="30" style="margin-right: 8px;">U</el-avatar>
							{{username}}
							<el-icon class="el-icon-right"><arrow-down /></el-icon>
						</span>
						
						<template #dropdown>
							<el-dropdown-menu>
								<el-dropdown-item command="profile">个人信息</el-dropdown-item>
								<el-dropdown-item command="logout" divided>退出登录</el-dropdown-item>
							</el-dropdown-menu>
						</template>
						
					</el-dropdown>
				</div>
				
			</el-header>
			
			<el-main class="main-content">
				<router-view />
			</el-main>
		</el-container>
		
	</el-container>
</template>

<script setup>
	import {computed, ref} from 'vue'
	import {useRoute,useRouter} from 'vue-router'
	import {useUserStore} from '../store/user.js'
	import { ElMessage, ElMessageBox } from 'element-plus'
	import { Document, List, OfficeBuilding, Bell, DataAnalysis, HomeFilled } from '@element-plus/icons-vue'
	import NotificationBell from '../components/NotificationBell.vue'
	
	const route = useRoute()
	const router = useRouter()
	const userStore = useUserStore()
	const unreadCount = ref(0)
	
	//自动高亮菜单
	const activeMenu = computed(() => route.path)
	
	//面包屑名映射
	const currentRouteName = computed(() => {
		return route.meta.title || '当前页'
	})
	
	const username = computed(() => userStore.userInfo?.fullName || userStore.userInfo?.username || '')
	
	const handleCommand = (command) =>{
		if(command === 'logout'){
			confirmLogout()
		} else if(command === 'profile'){
			router.push('/profile')
		}
	}
	
	const confirmLogout = () => {
		ElMessageBox.confirm('确定要退出系统吗？','提示',{
			confirmButtonText:'确定',
			cancelButtonText:'取消',
			type:'warning'
		}).then(() => {
			userStore.logout()
			route.replace('/login')
		})
	}
</script>

<style scoped>
	.layout-container {height: 100vh;}
	.logo {
	  height: 60px;
	  line-height: 60px;
	  text-align: center;
	  color: #fff;
	  font-weight: bold;
	  background: #2b2f3a;
	  font-size: 18px;
	}
	.header {
	  background: #fff;
	  border-bottom: 1px solid #dcdfe6;
	  display: flex;
	  align-items: center;
	  justify-content: space-between;
	}
	.header-right {
	  display: flex;
	  align-items: center;
	  gap: 15px;
	}
	.el-aside { background-color: #304156; }
	.main-content { background-color: #f0f2f5; padding: 20px; }
	.el-dropdown-link { cursor: pointer; display: flex; align-items: center; }
</style>