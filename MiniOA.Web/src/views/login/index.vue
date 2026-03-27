<template>
	<div class="login-container">
		<el-card class="login-card">
			<h2>用户登录</h2>
			
			<el-form :model="loginForm" :rules="rules" ref="loginRef" @keydown.enter.prevent="handleLogin">
				<el-form-item prop="username">
					<el-input v-model="loginForm.username" placeholder="请输入用户名">
						<template #prefix>
						    <el-icon><User /></el-icon>
						</template>
					</el-input>
				</el-form-item>
				
				<el-form-item prop="password">
					<el-input v-model="loginForm.password" type="password" placeholder="请输入密码" show-password >
						<template #prefix>
						    <el-icon><Lock /></el-icon>
						</template>
					</el-input>
				</el-form-item>
				
				<el-form-item>
					<el-button type="primary" :loading="loading" @click="handleLogin" style="width: 100%;">登  录</el-button>
				</el-form-item>
			</el-form>
		</el-card>
	</div>
</template>

<script setup>
	import { User,Lock } from '@element-plus/icons-vue'
	import {reactive,ref} from 'vue'
	import { useUserStore } from '../../store/user'
	import {useRouter} from 'vue-router'
	import { ElMessage } from 'element-plus'
	
	
	const router = useRouter()
	const userStore = useUserStore()
	const loading = ref(false)
	
	const loginForm = reactive({ username: '', password: ''})
	const rules = {
		username:[{required:true,message:'请输入用户名',trigger:'blur'}],
		password:[{required:true,message:'请输入密码',trigger:'blur'}]
	}
	
	const handleLogin = async () => {
		loading.value = true
		const success = await userStore.login(loginForm)
		loading.value = false
		
		if(success){
			ElMessage.success('欢迎回来')
			router.push('/')
		}
	}
</script>

<style scoped>
	.login-container{
		height: 100vh;
		display: flex;
		justify-content: center;
		align-items: center;
		background: #f3f4f6;
	}
	.login-card{width: 400px;padding: 20px;}
</style>