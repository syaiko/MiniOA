<template>
  <div class="my-applications">
    <el-card>
      <template #header>
        <span class="card-header">我的申请</span>
      </template>

      <el-table :data="applications" v-loading="loading" style="width: 100%">
        <el-table-column prop="title" label="标题" min-width="150" />
        <el-table-column prop="workflowType" label="流程类型" width="120">
          <template #default="{ row }">
            <el-tag :type="getWorkflowTypeTag(row.workflowType)" size="small">
              {{ getWorkflowTypeName(row.workflowType) }}
            </el-tag>
          </template>
        </el-table-column>
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
        <el-table-column prop="completedAt" label="完成时间" width="180">
          <template #default="{ row }">
            {{ row.completedAt ? formatDate(row.completedAt) : '-' }}
          </template>
        </el-table-column>
        <el-table-column label="操作" width="150" fixed="right">
          <template #default="{ row }">
            <el-button size="small" @click="handleViewDetail(row)">详情</el-button>
            <el-button 
              v-if="row.status === 0" 
              type="danger" 
              size="small" 
              @click="handleCancel(row)"
            >
              撤销
            </el-button>
            <el-button 
              v-if="row.status === 5" 
              type="primary" 
              size="small" 
              @click="handleReapply(row)"
            >
              重新申请
            </el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <el-dialog v-model="reapplyDialogVisible" title="重新申请" width="700px">
      <el-form ref="reapplyFormRef" :model="reapplyForm" :rules="reapplyRules" label-width="100px">
        <el-form-item label="申请标题" prop="title">
          <el-input v-model="reapplyForm.title" placeholder="请输入申请标题" />
        </el-form-item>

        <!-- 请假表单 -->
        <template v-if="reapplyForm.workflowType === 'Leave'">
          <el-form-item label="请假类型" prop="leaveType">
            <el-select v-model="reapplyForm.leaveType" placeholder="请选择请假类型">
              <el-option label="事假" value="事假" />
              <el-option label="病假" value="病假" />
              <el-option label="年假" value="年假" />
              <el-option label="调休" value="调休" />
            </el-select>
          </el-form-item>

          <el-form-item label="开始日期" prop="startDate">
            <el-date-picker
              v-model="reapplyForm.startDate"
              type="date"
              placeholder="选择开始日期"
              value-format="YYYY-MM-DD"
            />
          </el-form-item>

          <el-form-item label="结束日期" prop="endDate">
            <el-date-picker
              v-model="reapplyForm.endDate"
              type="date"
              placeholder="选择结束日期"
              value-format="YYYY-MM-DD"
            />
          </el-form-item>

          <el-form-item label="请假天数" prop="days">
            <el-input-number
              v-model="reapplyForm.days"
              :min="0.5"
              :max="30"
              :step="0.5"
              :precision="1"
              :disabled="true"
              placeholder="根据日期自动计算"
            />
          </el-form-item>

          <el-form-item label="请假原因" prop="reason">
            <el-input
              v-model="reapplyForm.reason"
              type="textarea"
              :rows="3"
              placeholder="请输入请假原因"
              maxlength="200"
              show-word-limit
            />
          </el-form-item>
        </template>

        <!-- 报销表单 -->
        <template v-if="reapplyForm.workflowType === 'Expense'">
          <el-form-item label="报销类型" prop="expenseType">
            <el-select v-model="reapplyForm.expenseType" placeholder="请选择报销类型">
              <el-option label="差旅费" value="差旅费" />
              <el-option label="交通费" value="交通费" />
              <el-option label="餐饮费" value="餐饮费" />
              <el-option label="办公用品" value="办公用品" />
              <el-option label="其他" value="其他" />
            </el-select>
          </el-form-item>

          <el-form-item label="报销金额" prop="amount">
            <el-input-number
              v-model="reapplyForm.amount"
              :min="0"
              :max="100000"
              :precision="2"
              :step="100"
            />
          </el-form-item>

          <el-form-item label="发票日期" prop="invoiceDate">
            <el-date-picker
              v-model="reapplyForm.invoiceDate"
              type="date"
              placeholder="选择发票日期"
              value-format="YYYY-MM-DD"
              :disabled-date="disabledDate"
            />
          </el-form-item>

          <el-form-item label="报销说明" prop="description">
            <el-input
              v-model="reapplyForm.description"
              type="textarea"
              :rows="3"
              placeholder="请输入报销说明"
              maxlength="200"
              show-word-limit
            />
          </el-form-item>
        </template>
      </el-form>
      <template #footer>
        <el-button @click="reapplyDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="reapplySubmitting" @click="handleReapplySubmit">确定</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, reactive, watch, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { getMyInstances, resubmitWorkflow } from '../../api/workflow'

const router = useRouter()
const loading = ref(false)
const applications = ref([])

const reapplyDialogVisible = ref(false)
const reapplySubmitting = ref(false)
const reapplyFormRef = ref(null)
const reapplyTarget = ref(null)
const reapplyForm = reactive({
  title: '',
  workflowType: 'Leave',
  // 请假字段
  leaveType: '',
  startDate: '',
  endDate: '',
  days: 0.5,
  reason: '',
  // 报销字段
  expenseType: '',
  amount: 0,
  invoiceDate: '',
  description: ''
})

// 重新申请校验规则
const reapplyRules = reactive({
  title: [
    { required: true, message: '请输入申请标题', trigger: 'blur' },
    { min: 2, max: 100, message: '标题长度在 2 到 100 个字符', trigger: 'blur' }
  ],
  // 请假校验
  leaveType: [
    { required: true, message: '请选择请假类型', trigger: 'change' }
  ],
  startDate: [
    { required: true, message: '请选择开始日期', trigger: 'change' }
  ],
  endDate: [
    { required: true, message: '请选择结束日期', trigger: 'change' },
    {
      validator: (rule, value, callback) => {
        if (value && reapplyForm.startDate && value < reapplyForm.startDate) {
          callback(new Error('结束日期不能早于开始日期'))
        } else {
          callback()
        }
      },
      trigger: 'change'
    }
  ],
  days: [
    { required: true, message: '请输入请假天数', trigger: 'blur' }
  ],
  reason: [
    { required: true, message: '请输入请假原因', trigger: 'blur' },
    { min: 5, max: 200, message: '原因长度在 5 到 200 个字符', trigger: 'blur' }
  ],
  // 报销校验
  expenseType: [
    { required: true, message: '请选择报销类型', trigger: 'change' }
  ],
  amount: [
    { required: true, message: '请输入报销金额', trigger: 'blur' },
    {
      validator: (rule, value, callback) => {
        if (value <= 0) {
          callback(new Error('报销金额必须大于0'))
        } else {
          callback()
        }
      },
      trigger: 'blur'
    }
  ],
  invoiceDate: [
    { required: true, message: '请选择发票日期', trigger: 'change' }
  ],
  description: [
    { required: true, message: '请输入报销说明', trigger: 'blur' },
    { min: 5, max: 200, message: '说明长度在 5 到 200 个字符', trigger: 'blur' }
  ]
})

const getWorkflowTypeTag = (type) => {
  const types = { 'Leave': 'primary', 'Expense': 'success' }
  return types[type] || 'info'
}

const getWorkflowTypeName = (type) => {
  const names = { 'Leave': '请假', 'Expense': '报销' }
  return names[type] || type
}

const getStatusTagType = (status) => {
  const types = {
    0: 'info',    // Pending
    1: 'warning', // InProgress
    2: 'success', // Approved
    3: 'rejected', // Rejected
    4: 'info',
    5: 'danger'
  }
  return types[status] || 'info'
}

const getStatusName = (status) => {
  const names = {
    0: '待审批',
    1: '审批中',
    2: '已批准',
    3: '已拒绝',
    4: '已撤销',
    5: '已退回'
  }
  return names[status] || status
}

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

const handleViewDetail = (row) => {
  router.push({ path: '/workflow/detail', query: { id: row.id } })
}

const handleCancel = (row) => {
  ElMessage.info('撤销功能待实现')
}

// 计算请假天数
const calculateReapplyDays = () => {
  if (reapplyForm.startDate && reapplyForm.endDate) {
    const start = new Date(reapplyForm.startDate)
    const end = new Date(reapplyForm.endDate)
    
    if (start && end && end >= start) {
      const timeDiff = end.getTime() - start.getTime()
      const daysDiff = Math.ceil(timeDiff / (1000 * 3600 * 24)) + 1
      reapplyForm.days = Math.min(Math.max(daysDiff, 0.5), 30)
    }
  }
}

// 监听日期变化
watch([() => reapplyForm.startDate, () => reapplyForm.endDate], () => {
  calculateReapplyDays()
})

// 禁用未来日期（发票日期不能超过今天）
const disabledDate = (time) => {
  return time.getTime() > Date.now()
}

const handleReapply = (row) => {
  reapplyTarget.value = row
  
  // 解析businessData
  let businessData = {}
  try {
    businessData = row.businessData ? JSON.parse(row.businessData) : {}
  } catch (e) {
    console.error('解析业务数据失败', e)
  }
  
  // 填充表单
  reapplyForm.title = row.title
  reapplyForm.workflowType = row.workflowType
  
  if (row.workflowType === 'Leave') {
    reapplyForm.leaveType = businessData.leaveType || ''
    reapplyForm.startDate = businessData.startDate || ''
    reapplyForm.endDate = businessData.endDate || ''
    reapplyForm.days = businessData.days || 0.5
    reapplyForm.reason = businessData.reason || ''
  } else if (row.workflowType === 'Expense') {
    reapplyForm.expenseType = businessData.expenseType || ''
    reapplyForm.amount = businessData.amount || 0
    reapplyForm.invoiceDate = businessData.invoiceDate || ''
    reapplyForm.description = businessData.description || ''
  }
  
  reapplyDialogVisible.value = true
}

const handleReapplySubmit = async () => {
  if (!reapplyFormRef.value || !reapplyTarget.value) return
  
  await reapplyFormRef.value.validate(async (valid) => {
    if (!valid) return
    
    reapplySubmitting.value = true
    try {
      // 构建业务数据
      const businessData = reapplyForm.workflowType === 'Leave'
        ? {
            leaveType: reapplyForm.leaveType,
            startDate: reapplyForm.startDate,
            endDate: reapplyForm.endDate,
            days: reapplyForm.days,
            reason: reapplyForm.reason
          }
        : {
            expenseType: reapplyForm.expenseType,
            amount: reapplyForm.amount,
            invoiceDate: reapplyForm.invoiceDate,
            description: reapplyForm.description
          }

      const payload = {
        title: reapplyForm.title,
        workflowType: reapplyForm.workflowType,
        businessData: JSON.stringify(businessData)
      }
      
      // 使用重新提交接口，传入原实例ID
      await resubmitWorkflow(reapplyTarget.value.id, payload)
      ElMessage.success('已重新提交')
      reapplyDialogVisible.value = false
      await loadApplications()
    } catch (error) {
      ElMessage.error('重新提交失败')
    } finally {
      reapplySubmitting.value = false
    }
  })
}

const loadApplications = async () => {
  loading.value = true
  try {
    const res = await getMyInstances()
    if (res && res.success) {
      applications.value = res.data || []
    }
  } catch (error) {
    ElMessage.error('加载申请列表失败')
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  loadApplications()
})
</script>

<style scoped>
.my-applications {
  padding: 20px;
}

.card-header {
  font-size: 18px;
  font-weight: bold;
}
</style>
