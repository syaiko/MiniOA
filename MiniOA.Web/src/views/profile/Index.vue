<template>
	<div class="container">
		<h1>个人档案</h1>
		
		<el-card class="profile-card">
			<template #header>
				<div class="card-header">
					<span>基本信息</span>
					<el-button type="primary" @click="handleEdit">编辑资料</el-button>
				</div>
			</template>
			
			<el-descriptions :column="2" border v-if="userInfo">
				<el-descriptions-item label="用户名">{{ userInfo.username }}</el-descriptions-item>
				<el-descriptions-item label="姓名">{{ userInfo.fullName || '-' }}</el-descriptions-item>
				<el-descriptions-item label="手机号">{{ userInfo.phone || '-' }}</el-descriptions-item>
				<el-descriptions-item label="邮箱">{{ userInfo.email || '-' }}</el-descriptions-item>
				<el-descriptions-item label="角色">
					<el-tag :type="getRoleTagType(userInfo.role)" size="small">
						{{ userInfo.roleDisplayName }}
					</el-tag>
				</el-descriptions-item>
				<el-descriptions-item label="部门">{{ userInfo.departmentName || '-' }}</el-descriptions-item>
				<el-descriptions-item label="状态">
					<el-tag :type="userInfo.isActive ? 'success' : 'danger'" size="small">
						{{ userInfo.isActive ? '启用' : '禁用' }}
					</el-tag>
				</el-descriptions-item>
				<el-descriptions-item label="创建时间">{{ formatDateTime(userInfo.createTime) }}</el-descriptions-item>
			</el-descriptions>
		</el-card>
		
		<el-card class="profile-card">
			<template #header>
				<div class="card-header">
					<span>安全设置</span>
				</div>
			</template>
			
			<div class="security-item">
				<div class="security-info">
					<div class="security-title">登录密码</div>
					<div class="security-desc">定期修改密码可以保护账户安全</div>
				</div>
				<el-button type="primary" @click="handleChangePassword">修改密码</el-button>
			</div>
		</el-card>
		
		<!-- 编辑资料对话框 -->
		<el-dialog v-model="showEditDialog" title="编辑个人资料" width="500px">
			<el-form :model="form" :rules="formRules" ref="formRef" label-width="80px">
				<el-form-item label="用户名">
					<el-input :value="userInfo?.username" disabled />
				</el-form-item>
				
				<el-form-item label="姓名" prop="fullName">
					<el-input v-model="form.fullName" placeholder="请输入姓名" />
				</el-form-item>
				
				<el-form-item label="手机号" prop="phone">
					<el-input v-model="form.phone" placeholder="请输入手机号" />
				</el-form-item>
				
				<el-form-item label="邮箱" prop="email">
					<el-input v-model="form.email" placeholder="请输入邮箱" />
				</el-form-item>
			</el-form>
			
			<template #footer>
				<el-button @click="showEditDialog = false">取消</el-button>
				<el-button type="primary" @click="handleSubmit" :loading="submitting">保存</el-button>
			</template>
		</el-dialog>
		
		<!-- 修改密码对话框 -->
		<el-dialog v-model="showPasswordDialog" title="修改密码" width="400px">
			<el-form :model="passwordForm" :rules="passwordRules" ref="passwordFormRef" label-width="100px">
				<el-form-item label="当前密码" prop="currentPassword">
					<el-input 
						v-model="passwordForm.currentPassword" 
						type="password" 
						placeholder="请输入当前密码"
						show-password
					/>
				</el-form-item>
				
				<el-form-item label="新密码" prop="newPassword">
					<el-input 
						v-model="passwordForm.newPassword" 
						type="password" 
						placeholder="请输入新密码（至少6位）"
						show-password
					/>
				</el-form-item>
				
				<el-form-item label="确认密码" prop="confirmPassword">
					<el-input 
						v-model="passwordForm.confirmPassword" 
						type="password" 
						placeholder="请再次输入新密码"
						show-password
					/>
				</el-form-item>
			</el-form>
			
			<template #footer>
				<el-button @click="showPasswordDialog = false">取消</el-button>
				<el-button type="primary" @click="handleSubmitPassword" :loading="submittingPassword">确认修改</el-button>
			</template>
		</el-dialog>
	</div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { getProfile, updateProfile, changePassword } from '../../api/user.js'

// 用户信息
const userInfo = ref(null)

// 编辑资料相关
const showEditDialog = ref(false)
const submitting = ref(false)
const formRef = ref()
const form = reactive({
	fullName: '',
	phone: '',
	email: ''
})

// 表单验证规则
const formRules = {
	fullName: [
		{ max: 100, message: '姓名长度不能超过100个字符', trigger: 'blur' }
	],
	phone: [
		{ pattern: /^1[3-9]\d{9}$/, message: '手机号格式不正确', trigger: 'blur' }
	],
	email: [
		{ pattern: /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/, message: '邮箱格式不正确', trigger: 'blur' }
	]
}

// 修改密码相关
const showPasswordDialog = ref(false)
const submittingPassword = ref(false)
const passwordFormRef = ref()
const passwordForm = reactive({
	currentPassword: '',
	newPassword: '',
	confirmPassword: ''
})

// 密码验证规则
const passwordRules = {
	currentPassword: [
		{ required: true, message: '请输入当前密码', trigger: 'blur' }
	],
	newPassword: [
		{ required: true, message: '请输入新密码', trigger: 'blur' },
		{ min: 6, message: '密码长度不能少于6个字符', trigger: 'blur' }
	],
	confirmPassword: [
		{ required: true, message: '请再次输入新密码', trigger: 'blur' },
		{ 
			validator: (rule, value, callback) => {
				if (value !== passwordForm.newPassword) {
					callback(new Error('两次输入的密码不一致'))
				} else {
					callback()
				}
			}, 
			trigger: 'blur' 
		}
	]
}

// 获取角色标签类型
const getRoleTagType = (role) => {
	const types = {
		'SuperAdmin': 'danger',
		'Admin': 'warning',
		'DepartmentManager': 'success',
		'TeamLeader': 'info',
		'Employee': ''
	}
	return types[role] || ''
}

// 格式化日期时间
const formatDateTime = (dateTime) => {
	if (!dateTime) return ''
	const date = new Date(dateTime)
	if (isNaN(date.getTime())) return '无效日期'
	
	return date.toLocaleString('zh-CN', {
		year: 'numeric',
		month: '2-digit',
		day: '2-digit',
		hour: '2-digit',
		minute: '2-digit',
		second: '2-digit',
		hour12: false
	}).replace(/\//g, '-')
}

// 获取个人资料
const fetchProfile = async () => {
	try {
		const res = await getProfile()
		userInfo.value = res
	} catch (error) {
		console.error('获取个人资料失败:', error)
	}
}

// 打开编辑对话框
const handleEdit = () => {
	if (userInfo.value) {
		form.fullName = userInfo.value.fullName || ''
		form.phone = userInfo.value.phone || ''
		form.email = userInfo.value.email || ''
	}
	showEditDialog.value = true
}

// 提交编辑
const handleSubmit = async () => {
	if (!formRef.value) return
	
	try {
		await formRef.value.validate()
	} catch {
		return
	}
	
	submitting.value = true
	try {
		const res = await updateProfile({
			fullName: form.fullName?.trim() || null,
			phone: form.phone?.trim() || null,
			email: form.email?.trim() || null
		})
		
		ElMessage.success('更新个人资料成功')
		userInfo.value = res
		showEditDialog.value = false
	} catch (error) {
		console.error('更新个人资料失败:', error)
	} finally {
		submitting.value = false
	}
}

// 打开修改密码对话框
const handleChangePassword = () => {
	passwordForm.currentPassword = ''
	passwordForm.newPassword = ''
	passwordForm.confirmPassword = ''
	showPasswordDialog.value = true
}

// 提交修改密码
const handleSubmitPassword = async () => {
	if (!passwordFormRef.value) return
	
	try {
		await passwordFormRef.value.validate()
	} catch {
		return
	}
	
	submittingPassword.value = true
	try {
		await changePassword(passwordForm.currentPassword, passwordForm.newPassword)
		ElMessage.success('修改密码成功')
		showPasswordDialog.value = false
	} catch (error) {
		console.error('修改密码失败:', error)
	} finally {
		submittingPassword.value = false
	}
}

onMounted(() => {
	fetchProfile()
})
</script>

<style scoped>
.container {
	padding: 20px;
}

.profile-card {
	margin-bottom: 20px;
	max-width: 800px;
}

.card-header {
	display: flex;
	justify-content: space-between;
	align-items: center;
}

.security-item {
	display: flex;
	justify-content: space-between;
	align-items: center;
	padding: 20px 0;
}

.security-info {
	flex: 1;
}

.security-title {
	font-size: 16px;
	font-weight: bold;
	margin-bottom: 5px;
}

.security-desc {
	font-size: 14px;
	color: #909399;
}
</style>
