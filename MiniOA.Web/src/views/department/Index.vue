<template>
  <div class="department-container">
    <div class="header">
      <h1>部门管理</h1>
      <el-button type="primary" @click="showAddDialog = true">
        <el-icon><Plus /></el-icon>
        新增部门
      </el-button>
    </div>

    <el-table :data="departmentList" border stripe row-key="id" :tree-props="{ children: 'children' }">
      <el-table-column prop="name" label="部门名称" min-width="200" />
      <el-table-column prop="fullPath" label="完整路径" min-width="300" />
      <el-table-column prop="managerName" label="部门负责人" width="120" />
      <el-table-column prop="level" label="层级" width="80" />
      <el-table-column prop="createdAt" label="创建时间" width="180">
        <template #default="scope">
          {{ formatDateTime(scope.row.createdAt) }}
        </template>
      </el-table-column>
      <el-table-column label="操作" width="200">
        <template #default="scope">
          <el-button size="small" @click="handleEdit(scope.row)">编辑</el-button>
          <el-button size="small" type="danger" @click="handleDelete(scope.row)">删除</el-button>
        </template>
      </el-table-column>
    </el-table>

    <!-- 新增/编辑部门对话框 -->
    <el-dialog 
      v-model="showAddDialog" 
      :title="editingDepartment ? '编辑部门' : '新增部门'" 
      width="500px"
      @close="resetForm"
    >
      <el-form :model="form" :rules="rules" ref="formRef" label-width="100px">
        <el-form-item label="部门名称" prop="name">
          <el-input v-model="form.name" placeholder="请输入部门名称" />
        </el-form-item>
        
        <el-form-item label="上级部门" prop="parentId">
          <el-tree-select
            v-model="form.parentId"
            :data="departmentTreeData"
            :props="{ value: 'id', label: 'name', children: 'children' }"
            placeholder="请选择上级部门"
            clearable
            check-strictly
            style="width: 100%"
          />
        </el-form-item>
        
        <el-form-item label="部门负责人" prop="managerId">
          <el-select v-model="form.managerId" placeholder="请选择部门负责人" style="width: 100%">
            <el-option
              v-for="user in userList"
              :key="user.id"
              :label="user.fullName || user.username"
              :value="user.id"
            />
          </el-select>
        </el-form-item>
        
        <el-form-item label="部门描述">
          <el-input v-model="form.description" type="textarea" :rows="3" placeholder="请输入部门描述" />
        </el-form-item>
      </el-form>
      
      <template #footer>
        <el-button @click="showAddDialog = false">取消</el-button>
        <el-button type="primary" @click="handleSubmit" :loading="submitting">确定</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted,watch } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus } from '@element-plus/icons-vue'
import { 
  getDepartmentTree, 
  createDepartment, 
  updateDepartment, 
  deleteDepartment,
  getDepartmentUsers
} from '../../api/department.js'

// 数据状态
const departmentList = ref([])
const departmentTreeData = ref([])
const userList = ref([])
const departmentUserList = ref([])
const showAddDialog = ref(false)
const submitting = ref(false)
const editingDepartment = ref(null)
const formRef = ref()

// 表单数据
const form = reactive({
  name: '',
  parentId: null,
  managerId: null,
  description: ''
})

// 表单验证规则
const rules = {
  name: [
    { required: true, message: '请输入部门名称', trigger: 'blur' },
    { min: 2, max: 100, message: '部门名称长度在 2 到 100 个字符', trigger: 'blur' }
  ],
  managerId: [
    { required: true, message: '请选择部门负责人', trigger: 'change' }
  ]
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

// 获取部门列表
const fetchDepartments = async () => {
  try {
    const res = await getDepartmentTree()
    // 处理 API 响应结构
    if (res && res.success) {
      departmentList.value = res.data || []
      departmentTreeData.value = res.data || []
    } else {
      departmentList.value = []
      departmentTreeData.value = []
    }
  } catch (error) {
    console.error('获取部门列表失败:', error)
    departmentList.value = []
    departmentTreeData.value = []
  }
}

//获取部门用户列表
const fetchDepartmentsUsers = async (deptId) => {
  try {
    const res = await getDepartmentUsers( deptId )
    // 处理 API 响应结构
    if (res && res.success) {
      userList.value = res.data || []
    } else {
      userList.value = []
    }
  } catch (error) {
    console.error('获取部门用户列表失败:', error)
    userList.value = []
  }
}

// 重置表单
const resetForm = () => {
  form.name = ''
  form.parentId = null
  form.managerId = null
  form.description = ''
  editingDepartment.value = null
  if (formRef.value) {
    formRef.value.resetFields()
  }
}

// 处理编辑
const handleEdit = (department) => {
  editingDepartment.value = department
  form.name = department.name
  form.parentId = department.parentId
  form.managerId = department.managerId
  form.description = department.description || ''
  showAddDialog.value = true
}

// 处理删除
const handleDelete = (department) => {
  ElMessageBox.confirm(
    `确定要删除部门 "${department.name}" 吗？`,
    '删除确认',
    {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    }
  ).then(async () => {
    try {
      await deleteDepartment(department.id)
      ElMessage.success('删除成功')
      await fetchDepartments()
    } catch (error) {
      console.error('删除部门失败:', error)
    }
  })
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
      name: form.name.trim(),
      parentId: form.parentId,
      managerId: form.managerId,
      description: form.description?.trim() || ''
    }

    if (editingDepartment.value) {
      await updateDepartment(editingDepartment.value.id, submitData)
      ElMessage.success('更新成功')
    } else {
      await createDepartment(submitData)
      ElMessage.success('创建成功')
    }

    showAddDialog.value = false
    resetForm()
    await fetchDepartments()
  } catch (error) {
    console.error('提交失败:', error)
  } finally {
    submitting.value = false
  }
}

onMounted(() => {
  fetchDepartments()
})

watch(() => form.parentId,async (newVal) => {
	if(newVal){
		await fetchDepartmentsUsers(newVal)
	}else{
		userList.value = []  //清空
		return
	}
})

</script>

<style scoped>
.department-container {
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
</style>
