<template>
  <div class="workflow-workbench">
    <el-card>
      <template #header>
        <span class="card-header">待我审批</span>
      </template>

      <el-table
        :data="todoList"
        v-loading="loading"
        style="width: 100%"
      >
        <el-table-column prop="title" label="标题" min-width="150" />
        <el-table-column prop="workflowType" label="流程类型" width="120">
          <template #default="{ row }">
            <el-tag :type="getWorkflowTypeTag(row.workflowType)" size="small">
              {{ getWorkflowTypeName(row.workflowType) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="creatorName" label="申请人" width="120" />
        <el-table-column prop="currentNodeName" label="当前节点" width="120" />
        <el-table-column prop="createdAt" label="申请时间" width="180">
          <template #default="{ row }">
            {{ formatDate(row.createdAt) }}
          </template>
        </el-table-column>
        <el-table-column prop="status" label="状态" width="100">
          <template #default="{ row }">
            <el-tag :type="getStatusTagType(row.status)" size="small">
              {{ getStatusName(row.status) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="200" fixed="right">
          <template #default="{ row }">
            <el-button type="primary" size="small" @click="handleApprove(row)">
              审批
            </el-button>
            <el-button size="small" @click="handleViewDetail(row)">
              详情
            </el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <!-- 审批对话框 -->
    <el-dialog v-model="approveDialogVisible" title="审批" width="500px">
      <el-form :model="approveForm" label-width="80px">
        <el-form-item label="审批意见">
          <el-radio-group v-model="approveForm.action">
            <el-radio-button value="Approve">批准</el-radio-button>
            <el-radio-button value="Reject">拒绝</el-radio-button>
            <el-radio-button value="Return">退回</el-radio-button>
          </el-radio-group>
        </el-form-item>
        <el-form-item label="意见说明">
          <el-input
            v-model="approveForm.comment"
            type="textarea"
            :rows="3"
            placeholder="请输入审批意见"
            maxlength="200"
            show-word-limit
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="approveDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSubmitApprove" :loading="submitting">
          确定
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { getMyTodo, approveWorkflow } from '../../api/workflow'

const router = useRouter()
const loading = ref(false)
const submitting = ref(false)
const todoList = ref([])
const approveDialogVisible = ref(false)
const currentInstance = ref(null)

const approveForm = ref({
  instanceId: null,
  action: 'Approve',
  comment: ''
})

// 获取流程类型标签
const getWorkflowTypeTag = (type) => {
  const types = {
    'Leave': 'primary',
    'Expense': 'success'
  }
  return types[type] || 'info'
}

// 获取流程类型名称
const getWorkflowTypeName = (type) => {
  const names = {
    'Leave': '请假',
    'Expense': '报销'
  }
  return names[type] || type
}

// 获取状态标签类型
const getStatusTagType = (status) => {
  const types = {
    0: 'info',    // Pending
    1: 'warning', // InProgress
    2: 'success', // Approved
    3: 'danger'   // Rejected
  }
  return types[status] || 'info'
}

// 获取状态名称
const getStatusName = (status) => {
  const names = {
    0: '待审批',
    1: '审批中',
    2: '已批准',
    3: '已拒绝'
  }
  return names[status] || status
}

// 格式化日期
const formatDate = (dateStr) => {
  const date = new Date(dateStr)
  return date.toLocaleString('zh-CN', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit'
  })
}

// 加载待办列表
const loadTodoList = async () => {
  loading.value = true
  try {
    const res = await getMyTodo()
    if (res && res.success) {
      todoList.value = res.data || []
    } else {
      todoList.value = []
    }
  } catch (error) {
    ElMessage.error('加载待办列表失败')
  } finally {
    loading.value = false
  }
}

// 审批
const handleApprove = (row) => {
  currentInstance.value = row
  approveForm.value = {
    instanceId: row.id,
    action: 'Approve',
    comment: ''
  }
  approveDialogVisible.value = true
}

// 提交审批
const handleSubmitApprove = async () => {
  submitting.value = true
  try {
    // 映射 action 字符串到数字枚举值
    const actionMap = {
      'Approve': 1,    // ApprovalAction.Approve
      'Reject': 2,     // ApprovalAction.Reject  
      'Return': 3       // ApprovalAction.Return
    }
    
    const submitData = {
      instanceId: approveForm.value.instanceId,
      action: actionMap[approveForm.value.action] || 1,
      comment: approveForm.value.comment
    }
    
    const res = await approveWorkflow(submitData)
    if (res && res.success) {
      ElMessage.success('审批成功')
      approveDialogVisible.value = false
      loadTodoList()
    } else {
      ElMessage.error((res && res.message) || '审批失败')
    }
  } catch (error) {
    ElMessage.error('审批失败，请稍后重试')
  } finally {
    submitting.value = false
  }
}

// 查看详情
const handleViewDetail = (row) => {
  router.push({
    path: '/workflow/detail',
    query: { id: row.id }
  })
}

onMounted(() => {
  loadTodoList()
})
</script>

<style scoped>
.workflow-workbench {
  padding: 20px;
}

.card-header {
  font-size: 18px;
  font-weight: bold;
}

.tab-badge {
  margin-left: 5px;
}

.el-table {
  margin-top: 20px;
}
</style>
