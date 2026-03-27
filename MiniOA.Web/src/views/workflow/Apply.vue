<template>
  <div class="workflow-apply">
    <el-card>
      <template #header>
        <span class="card-header">提交申请</span>
      </template>

      <!-- 申请类型切换 -->
      <el-radio-group v-model="form.workflowType" @change="handleTypeChange">
        <el-radio-button value="Leave">请假申请</el-radio-button>
        <el-radio-button value="Expense">报销申请</el-radio-button>
      </el-radio-group>

      <el-divider />

      <!-- 申请表单 -->
      <el-form
        ref="formRef"
        :model="form"
        :rules="rules"
        label-width="100px"
        style="max-width: 600px"
      >
        <el-form-item label="申请标题" prop="title">
          <el-input v-model="form.title" placeholder="请输入申请标题" />
        </el-form-item>

        <!-- 请假表单 -->
        <template v-if="form.workflowType === 'Leave'">
          <el-form-item label="请假类型" prop="leaveType">
            <el-select v-model="form.leaveType" placeholder="请选择请假类型">
              <el-option label="事假" value="事假" />
              <el-option label="病假" value="病假" />
              <el-option label="年假" value="年假" />
              <el-option label="调休" value="调休" />
            </el-select>
          </el-form-item>

          <el-form-item label="开始日期" prop="startDate">
            <el-date-picker
              v-model="form.startDate"
              type="date"
              placeholder="选择开始日期"
              value-format="YYYY-MM-DD"
            />
          </el-form-item>

          <el-form-item label="结束日期" prop="endDate">
            <el-date-picker
              v-model="form.endDate"
              type="date"
              placeholder="选择结束日期"
              value-format="YYYY-MM-DD"
            />
          </el-form-item>

          <el-form-item label="请假天数" prop="days">
            <el-input-number
              v-model="form.days"
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
              v-model="form.reason"
              type="textarea"
              :rows="3"
              placeholder="请输入请假原因"
              maxlength="200"
              show-word-limit
            />
          </el-form-item>
        </template>

        <!-- 报销表单 -->
        <template v-if="form.workflowType === 'Expense'">
          <el-form-item label="报销类型" prop="expenseType">
            <el-select v-model="form.expenseType" placeholder="请选择报销类型">
              <el-option label="差旅费" value="差旅费" />
              <el-option label="交通费" value="交通费" />
              <el-option label="餐饮费" value="餐饮费" />
              <el-option label="办公用品" value="办公用品" />
              <el-option label="其他" value="其他" />
            </el-select>
          </el-form-item>

          <el-form-item label="报销金额" prop="amount">
            <el-input-number
              v-model="form.amount"
              :min="0"
              :max="100000"
              :precision="2"
              :step="100"
            />
          </el-form-item>

          <el-form-item label="发票日期" prop="invoiceDate">
            <el-date-picker
              v-model="form.invoiceDate"
              type="date"
              placeholder="选择发票日期"
              value-format="YYYY-MM-DD"
              :disabled-date="disabledDate"
            />
          </el-form-item>

          <el-form-item label="报销说明" prop="description">
            <el-input
              v-model="form.description"
              type="textarea"
              :rows="3"
              placeholder="请输入报销说明"
              maxlength="200"
              show-word-limit
            />
          </el-form-item>
        </template>

        <el-form-item>
          <el-button type="primary" @click="handleSubmit" :loading="loading">
            提交申请
          </el-button>
          <el-button @click="handleReset">重置</el-button>
        </el-form-item>
      </el-form>
    </el-card>
  </div>
</template>

<script setup>
import { ref, reactive, watch } from 'vue'
import { ElMessage } from 'element-plus'
import { submitWorkflow } from '../../api/workflow'

const formRef = ref(null)
const loading = ref(false)

const form = reactive({
  workflowType: 'Leave',
  title: '',
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

// 校验规则
const rules = reactive({
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
        if (value && form.startDate && value < form.startDate) {
          callback(new Error('结束日期不能早于开始日期'))
        } else {
          callback()
        }
      },
      trigger: 'change'
    }
  ],
  days: [
    { required: true, message: '请输入请假天数', trigger: 'blur' },
    {
      validator: (rule, value, callback) => {
        if (value <= 0) {
          callback(new Error('请假天数必须大于0'))
        } else if (value > 30) {
          callback(new Error('单次请假天数不能超过30天'))
        } else {
          callback()
        }
      },
      trigger: 'blur'
    }
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
        } else if (value > 100000) {
          callback(new Error('单次报销金额不能超过100000元'))
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

// 计算请假天数
const calculateDays = () => {
  if (form.startDate && form.endDate) {
    const start = new Date(form.startDate)
    const end = new Date(form.endDate)
    
    if (start && end && end >= start) {
      // 计算天数差（包括周末）
      const timeDiff = end.getTime() - start.getTime()
      const daysDiff = Math.ceil(timeDiff / (1000 * 3600 * 24)) + 1
      
      // 确保最小0.5天，最大30天
      form.days = Math.min(Math.max(daysDiff, 0.5), 30)
    }
  }
}

// 监听日期变化
watch([() => form.startDate, () => form.endDate], () => {
  calculateDays()
})

// 切换申请类型
const handleTypeChange = () => {
  formRef.value?.resetFields()
  form.title = ''
  form.leaveType = ''
  form.startDate = ''
  form.endDate = ''
  form.days = 0.5
  form.reason = ''
  form.expenseType = ''
  form.amount = 0
  form.invoiceDate = ''
  form.description = ''
}

// 禁用未来日期（发票日期不能超过今天）
const disabledDate = (time) => {
  return time.getTime() > Date.now()
}

// 提交申请
const handleSubmit = async () => {
  if (!formRef.value) return

  await formRef.value.validate(async (valid) => {
    if (!valid) return

    loading.value = true
    try {
      // 构建业务数据
      const businessData = form.workflowType === 'Leave'
        ? {
            leaveType: form.leaveType,
            startDate: form.startDate,
            endDate: form.endDate,
            days: form.days,
            reason: form.reason
          }
        : {
            expenseType: form.expenseType,
            amount: form.amount,
            invoiceDate: form.invoiceDate,
            description: form.description
          }

      const submitData = {
        title: form.title,
        workflowType: form.workflowType, // 这里的 Leave/Expense 必须与后端枚举名一致
        businessData: JSON.stringify(businessData)
      }

      const res = await submitWorkflow(submitData)
      
      // 因为拦截器返回的是完整的 ApiResult 对象 (含 success, message, data)
      if (res && res.success) {
        ElMessage.success('申请提交成功')
        handleReset()
      } else {
        const errorMsg = res?.message || '提交失败'
        ElMessage.error(errorMsg)
      }
    } catch (error) {
      ElMessage.error('系统异常: ' + (error.message || '未知错误'))
    } finally {
      loading.value = false
    }
  })
}

// 重置表单
const handleReset = () => {
  formRef.value?.resetFields()
  form.title = ''
  form.leaveType = ''
  form.startDate = ''
  form.endDate = ''
  form.days = 0.5
  form.reason = ''
  form.expenseType = ''
  form.amount = 0
  form.invoiceDate = ''
  form.description = ''
}
</script>

<style scoped>
.workflow-apply {
  padding: 20px;
}

.card-header {
  font-size: 18px;
  font-weight: bold;
}

.el-radio-group {
  margin-bottom: 20px;
}

.el-divider {
  margin: 20px 0;
}
</style>
