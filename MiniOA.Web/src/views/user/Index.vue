<template>
  <div class="user-container">
    <div class="header">
      <h1>用户管理</h1>
    </div>
	
	<div class="operationUser">
		<div class="header-actions">
			<div class="left">
				<el-input
				  v-model="searchKeyword" 
				  placeholder="搜索用户名或姓名" 
				  clearable 
				  style="width: 200px;"
				  @keyup.enter="handleSearch"
				/>
				<el-button type="primary" @click="handleSearch" style="width: 80px;">
				  搜索
				</el-button>
				
				<el-select v-model="selectedDepartment" placeholder="筛选部门" clearable style="width: 200px; margin-left: 30px">
				  <el-option
				    v-for="dept in departmentOptions"
				    :key="dept.id"
				    :label="dept.fullPath"
				    :value="dept.id"
				  />
				</el-select>
				
			</div>
		    <div class="right">
				<el-button type="primary" @click="showAddDialog = true">
				  <el-icon><Plus /></el-icon>
				  新增用户
				</el-button>
			</div>
		</div>
	</div>

    <el-table :data="userList" border stripe>
      <el-table-column prop="username" label="用户名" width="120" />
      <el-table-column prop="fullName" label="姓名" width="120" />
      <el-table-column prop="roleDisplayName" label="角色" width="120">
        <template #default="scope">
          <el-tag :type="getRoleTagType(scope.row.role)" size="small">
            {{ scope.row.roleDisplayName }}
          </el-tag>
        </template>
      </el-table-column>
      <el-table-column prop="departmentFullPath" label="部门" min-width="200" />
      <el-table-column prop="isActive" label="状态" width="80">
        <template #default="scope">
          <el-tag :type="scope.row.isActive ? 'success' : 'danger'" size="small">
            {{ scope.row.isActive ? '启用' : '禁用' }}
          </el-tag>
        </template>
      </el-table-column>
      <el-table-column prop="createTime" label="创建时间" width="180">
        <template #default="scope">
          {{ formatDateTime(scope.row.createTime) }}
        </template>
      </el-table-column>
      <el-table-column label="操作" width="350">
        <template #default="scope">
			<el-space>
				<el-button size="small" @click="handleEdit(scope.row)">编辑</el-button>
				<el-button size="small" type="info" @click="handleResetPassword(scope.row)">重置密码</el-button>
				<el-button 
				  size="small" 
				  :type="scope.row.isActive ? 'warning' : 'success'"
				  @click="handleToggleStatus(scope.row)"
				>
				  {{ scope.row.isActive ? '禁用' : '启用' }}
				</el-button>
				<el-button size="small" type="primary" @click="handleViewDetail(scope.row)">详细</el-button>
				<el-button size="small" type="danger" @click="handleDelete(scope.row)">删除</el-button>
			</el-space>
        </template>
      </el-table-column>
    </el-table>

    <!-- 新增/编辑用户对话框 -->
    <el-dialog 
      v-model="showAddDialog" 
      :title="editingUser ? '编辑用户' : '新增用户'" 
      width="500px"
      @close="resetForm"
    >
      <el-form :model="form" :rules="rules" ref="formRef" label-width="100px">
        <el-form-item label="用户名" prop="username">
          <el-input v-model="form.username" placeholder="请输入用户名" :disabled="!!editingUser" />
        </el-form-item>
        
        <el-form-item label="密码" prop="password" v-if="!editingUser">
          <el-input v-model="form.password" type="password" placeholder="请输入密码" />
        </el-form-item>
        
        <el-form-item label="姓名" prop="fullName">
          <el-input v-model="form.fullName" placeholder="请输入姓名" />
        </el-form-item>
        
        <el-form-item label="角色" prop="role">
          <el-select v-model="form.role" placeholder="请选择角色" style="width: 100%">
            <el-option
              v-for="role in roleOptions"
              :key="role.value"
              :label="role.label"
              :value="role.value"
            />
          </el-select>
        </el-form-item>
        
        <el-form-item label="所属部门" prop="departmentId">
          <el-tree-select
            v-model="form.departmentId"
            :data="departmentTreeData"
            :props="{ value: 'id', label: 'fullPath', children: 'children' }"
            placeholder="请选择所属部门"
            clearable
            check-strictly
            style="width: 100%"
          />
        </el-form-item>
      </el-form>
      
      <template #footer>
        <el-button @click="showAddDialog = false">取消</el-button>
        <el-button type="primary" @click="handleSubmit" :loading="submitting">确定</el-button>
      </template>
    </el-dialog>

    <!-- 重置密码对话框 -->
    <el-dialog 
      v-model="showResetPasswordDialog" 
      title="重置密码" 
      width="400px"
    >
      <el-form :model="resetPasswordForm" :rules="resetPasswordRules" ref="resetPasswordRef" label-width="80px">
        <el-form-item label="用户">
          <el-input :value="resetPasswordForm.username" disabled />
        </el-form-item>
        
        <el-form-item label="新密码" prop="newPassword">
          <el-input 
            v-model="resetPasswordForm.newPassword" 
            type="password" 
            placeholder="请输入新密码"
            show-password
          />
        </el-form-item>
      </el-form>
      
      <template #footer>
        <el-button @click="showResetPasswordDialog = false">取消</el-button>
        <el-button type="primary" @click="handleResetPasswordSubmit" :loading="resettingPassword">确定</el-button>
      </template>
    </el-dialog>

    <!-- 用户详情对话框 -->
    <el-dialog 
      v-model="showDetailDialog" 
      title="用户详情" 
      width="500px"
    >
      <el-descriptions v-if="detailUser" :column="1" border>
        <el-descriptions-item label="用户名">{{ detailUser.username }}</el-descriptions-item>
        <el-descriptions-item label="姓名">{{ detailUser.fullName || '-' }}</el-descriptions-item>
        <el-descriptions-item label="角色">
          <el-tag :type="getRoleTagType(detailUser.role)" size="small">
            {{ detailUser.roleDisplayName }}
          </el-tag>
        </el-descriptions-item>
        <el-descriptions-item label="部门">{{ detailUser.departmentFullPath || '-' }}</el-descriptions-item>
        <el-descriptions-item label="状态">
          <el-tag :type="detailUser.isActive ? 'success' : 'danger'" size="small">
            {{ detailUser.isActive ? '启用' : '禁用' }}
          </el-tag>
        </el-descriptions-item>
        <el-descriptions-item label="创建时间">{{ formatDateTime(detailUser.createTime) }}</el-descriptions-item>
      </el-descriptions>
      
      <template #footer>
        <el-button @click="showDetailDialog = false">关闭</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted, watch } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus } from '@element-plus/icons-vue'
import { 
  getUsers, 
  getUser,
  createUser, 
  updateUser, 
  deleteUser,
  resetPassword 
} from '../../api/user.js'
import { getDepartmentTree } from '../../api/department.js'

// 数据状态
const allUsers = ref([]) // 存储所有用户数据
const userList = ref([]) // 显示的用户列表（筛选后的）
const departmentTreeData = ref([])
const departmentOptions = ref([])
const selectedDepartment = ref(null)
const searchKeyword = ref('')
const showAddDialog = ref(false)
const submitting = ref(false)
const editingUser = ref(null)
const formRef = ref()

// 重置密码相关
const showResetPasswordDialog = ref(false)
const resettingPassword = ref(false)
const resetPasswordRef = ref()
const resetPasswordForm = reactive({
  userId: null,
  username: '',
  newPassword: ''
})

// 用户详情相关
const showDetailDialog = ref(false)
const detailUser = ref(null)

// 重置密码表单验证规则
const resetPasswordRules = {
  newPassword: [
    { required: true, message: '请输入新密码', trigger: 'blur' },
    { min: 6, message: '密码长度不能少于6个字符', trigger: 'blur' }
  ]
}

// 角色选项
const roleOptions = [
  { label: '超级管理员', value: 0 },
  { label: '管理员', value: 1 },
  { label: '部门经理', value: 2 },
  { label: '组长', value: 3 },
  { label: '普通员工', value: 4 },
  { label: '实习生', value: 5 }
]

// 表单数据
const form = reactive({
  username: '',
  password: '',
  fullName: '',
  role: 4, // 默认普通员工
  departmentId: null
})

// 表单验证规则
const rules = {
  username: [
    { required: true, message: '请输入用户名', trigger: 'blur' },
    { min: 3, max: 50, message: '用户名长度在 3 到 50 个字符', trigger: 'blur' }
  ],
  password: [
    { required: true, message: '请输入密码', trigger: 'blur' },
    { min: 6, message: '密码长度不能少于6个字符', trigger: 'blur' }
  ],
  role: [
    { required: true, message: '请选择角色', trigger: 'change' }
  ]
}

// 获取角色标签类型
const getRoleTagType = (role) => {
  const typeMap = {
    0: 'danger', // 超级管理员
    1: 'warning', // 管理员
    2: 'primary', // 部门经理
    3: 'info', // 组长
    4: '', // 普通员工
    5: 'info' // 实习生
  }
  return typeMap[role] || ''
}

// 格式化日期时间
const formatDateTime = (dateTime) => {
  if (!dateTime) return ''
  const date = new Date(dateTime)
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

// 获取用户数据（部门筛选调用后端）
const fetchUsers = async () => {
  try {
    const res = await getUsers(selectedDepartment.value)
    // 处理 API 响应结构
    if (res && res.success) {
      allUsers.value = res.data || []
    } else {
      allUsers.value = []
    }
    applyFilters() // 应用本地搜索筛选
  } catch (error) {
    console.error('获取用户列表失败:', error)
    allUsers.value = []
    applyFilters()
  }
}

// 应用本地搜索筛选
const applyFilters = () => {
  let filtered = [...allUsers.value]
  
  // 搜索筛选（用户名或姓名）
  if (searchKeyword.value && searchKeyword.value.trim()) {
    const searchTerm = searchKeyword.value.trim().toLowerCase()
    filtered = filtered.filter(user => 
      user.username.toLowerCase().includes(searchTerm) ||
      (user.fullName && user.fullName.toLowerCase().includes(searchTerm))
    )
  }
  
  userList.value = filtered
}

// 搜索处理函数（点击搜索按钮触发本地筛选）
const handleSearch = () => {
  applyFilters()
}

// 获取部门列表
const fetchDepartments = async () => {
  try {
    const res = await getDepartmentTree()
    // 处理 API 响应结构
    if (res && res.success) {
      departmentTreeData.value = res.data || []
      
      // 扁平化部门数据用于下拉选择
      const flattenDepts = (depts) => {
        const result = []
        depts.forEach(dept => {
          result.push(dept)
          if (dept.children && dept.children.length > 0) {
            result.push(...flattenDepts(dept.children))
          }
        })
        return result
      }
      departmentOptions.value = flattenDepts(res.data || [])
    } else {
      departmentTreeData.value = []
      departmentOptions.value = []
    }
  } catch (error) {
    console.error('获取部门列表失败:', error)
    departmentTreeData.value = []
    departmentOptions.value = []
  }
}

// 重置表单
const resetForm = () => {
  form.username = ''
  form.password = ''
  form.fullName = ''
  form.role = 4
  form.departmentId = null
  editingUser.value = null
  if (formRef.value) {
    formRef.value.resetFields()
  }
}

// 处理编辑
const handleEdit = (user) => {
  editingUser.value = user
  form.username = user.username
  form.fullName = user.fullName
  form.role = user.role
  form.departmentId = user.departmentId
  showAddDialog.value = true
}

// 处理删除
const handleDelete = (user) => {
  ElMessageBox.confirm(
    `确定要删除用户 "${user.fullName || user.username}" 吗？`,
    '删除确认',
    {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    }
  ).then(async () => {
    try {
      await deleteUser(user.id)
      ElMessage.success('删除成功')
      await fetchUsers()
    } catch (error) {
      console.error('删除用户失败:', error)
    }
  })
}

// 处理切换状态
const handleToggleStatus = (user) => {
  const action = user.isActive ? '禁用' : '启用'
  ElMessageBox.confirm(
    `确定要${action}用户 "${user.fullName || user.username}" 吗？`,
    `${action}确认`,
    {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    }
  ).then(async () => {
    try {
      await updateUser(user.id, {
        fullName: user.fullName,
        role: user.role,
        departmentId: user.departmentId,
        isActive: !user.isActive
      })
      ElMessage.success(`${action}成功`)
      await fetchUsers()
    } catch (error) {
      console.error(`${action}用户失败:`, error)
    }
  })
}

// 处理查看详情
const handleViewDetail = async (user) => {
  try {
    const res = await getUser(user.id)
    detailUser.value = res
    showDetailDialog.value = true
  } catch (error) {
    console.error('获取用户详情失败:', error)
    ElMessage.error('获取用户详情失败')
  }
}

// 处理重置密码
const handleResetPassword = (user) => {
  resetPasswordForm.userId = user.id
  resetPasswordForm.username = user.fullName || user.username
  resetPasswordForm.newPassword = ''
  showResetPasswordDialog.value = true
}

// 处理重置密码提交
const handleResetPasswordSubmit = async () => {
  if (!resetPasswordRef.value) return
  
  try {
    await resetPasswordRef.value.validate()
  } catch {
    return
  }

  resettingPassword.value = true
  try {
    await resetPassword(resetPasswordForm.userId, resetPasswordForm.newPassword)
    ElMessage.success('重置密码成功')
    showResetPasswordDialog.value = false
    resetPasswordForm.newPassword = ''
  } catch (error) {
    console.error('重置密码失败:', error)
  } finally {
    resettingPassword.value = false
  }
}

// 处理提交
const handleSubmit = async () => {
  if (!formRef.value) return
  
  try {
    await formRef.value.validate()
  } catch {
    return
  }

  submitting.value = true
  try {
    const submitData = {
      username: form.username.trim(),
      fullName: form.fullName?.trim() || '',
      role: form.role,
      departmentId: form.departmentId
    }

    if (!editingUser.value) {
      submitData.password = form.password
      await createUser(submitData)
      ElMessage.success('创建成功')
    } else {
      await updateUser(editingUser.value.id, submitData)
      ElMessage.success('更新成功')
    }

    showAddDialog.value = false
    resetForm()
    await fetchUsers()
  } catch (error) {
    console.error('提交失败:', error)
  } finally {
    submitting.value = false
  }
}

// 监听部门筛选变化（调用后端查询）
watch(selectedDepartment, () => {
  fetchUsers()
})

onMounted(() => {
  fetchDepartments()
  fetchUsers()
})
</script>

<style scoped>
.user-container {
  padding: 20px;
}

.header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
}

.header h1 {
  margin: 0;
  font-size: 24px;
  color: #303133;
}

.header-actions {
  display: flex;
  justify-content: space-between;
  align-items: center;
  width: 100%;   
}

.operationUser{
	display: flex;
	justify-content: space-between; /* 左右分开 */
	align-items: center;
	margin-bottom: 10px;
}
.left{
	display: flex;
	gap: 10px;
}
</style>
