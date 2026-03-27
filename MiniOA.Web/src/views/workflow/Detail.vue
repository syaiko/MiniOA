<template>
  <div class="workflow-detail">
    <el-card v-loading="loading">
      <template #header>
        <div class="detail-header">
          <span class="title">工作流详情</span>
          <el-button @click="goBack">返回</el-button>
        </div>
      </template>

      <div v-if="instance" class="detail-content">
        <!-- 基本信息 -->
        <el-descriptions title="基本信息" :column="2" border>
          <el-descriptions-item label="标题">{{ instance.title }}</el-descriptions-item>
          <el-descriptions-item label="流程类型">
            <el-tag :type="getWorkflowTypeTag(instance.workflowType)" size="small">
              {{ getWorkflowTypeName(instance.workflowType) }}
            </el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="申请人">{{ instance.creatorName }}</el-descriptions-item>
          <el-descriptions-item label="申请时间">{{ formatDate(instance.createdAt) }}</el-descriptions-item>
          <el-descriptions-item label="当前状态">
            <el-tag :type="getStatusTagType(instance.status)" size="small">
              {{ getStatusName(instance.status) }}
            </el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="当前进度">{{ instance.currentNodeName || '-' }}</el-descriptions-item>
        </el-descriptions>

        <!-- 业务数据 -->
        <div class="business-data" v-if="businessData">
          <h3>申请内容</h3>
          <el-descriptions :column="2" border>
            <template v-if="instance.workflowType === 'Leave'">
              <el-descriptions-item label="请假类型">{{ businessData.leaveType }}</el-descriptions-item>
              <el-descriptions-item label="请假天数">{{ businessData.days }}天</el-descriptions-item>
              <el-descriptions-item label="开始时间">{{ businessData.startDate }}</el-descriptions-item>
              <el-descriptions-item label="结束时间">{{ businessData.endDate }}</el-descriptions-item>
              <el-descriptions-item label="请假原因" :span="2">{{ businessData.reason }}</el-descriptions-item>
            </template>
            <template v-else-if="instance.workflowType === 'Expense'">
              <el-descriptions-item label="费用类型">{{ businessData.expenseType }}</el-descriptions-item>
              <el-descriptions-item label="金额">¥{{ businessData.amount }}</el-descriptions-item>
              <el-descriptions-item label="发票日期">{{ businessData.invoiceDate }}</el-descriptions-item>
              <el-descriptions-item label="费用说明" :span="2">{{ businessData.description }}</el-descriptions-item>
            </template>
          </el-descriptions>
        </div>

        <!-- 审批记录 -->
        <div class="approval-records">
          <h3>审批记录</h3>
          <el-table :data="approvalRecords" style="width: 100%">
            <el-table-column prop="nodeName" label="审批节点" width="150" />
            <el-table-column prop="approverName" label="审批人" width="120" />
            <el-table-column prop="action" label="审批动作" width="100">
              <template #default="{ row }">
                <el-tag :type="getActionTagType(row.action)" size="small">
                  {{ getActionName(row.action) }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="comment" label="审批意见" min-width="200" />
            <el-table-column prop="createdAt" label="审批时间" width="180">
              <template #default="{ row }">
                {{ formatDate(row.createdAt) }}
              </template>
            </el-table-column>
          </el-table>
        </div>
      </div>
    </el-card>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { getWorkflowInstance } from '../../api/workflow'

const route = useRoute()
const router = useRouter()

const loading = ref(false)
const instance = ref(null)
const businessData = ref(null)
const approvalRecords = ref([])

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
    0: 'info',     // Pending
    1: 'warning',  // InProgress
    2: 'success',  // Approved
    3: 'danger',   // Rejected
    4: 'info'      // Cancelled
  }
  return types[status] || 'info'
}

// 获取状态名称
const getStatusName = (status) => {
  const names = {
    0: '待审批',    // Pending - 等待审批
    1: '进行中',    // InProgress - 审批中
    2: '已通过',    // Approved - 已通过
    3: '已拒绝',    // Rejected - 已拒绝
    4: '已退回'     // Return - 已退回
  }
  return names[status] || '未知'
}

// 获取动作标签类型
const getActionTagType = (action) => {
  const types = {
    0: 'info',     // Submit
    1: 'success',  // Approve
    2: 'danger',   // Reject
    3: 'warning',  // Return
    4: 'info'      // Cancel
  }
  return types[action] || 'info'
}

// 获取动作名称
const getActionName = (action) => {
  const names = {
    0: '提交',
    1: '批准',
    2: '驳回',
    3: '退回',
    4: '取消'
  }
  return names[action] || '未知'
}

// 格式化日期
const formatDate = (dateString) => {
  if (!dateString) return '-'
  const date = new Date(dateString)
  return date.toLocaleString('zh-CN', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit'
  })
}

// 返回
const goBack = () => {
  router.go(-1)
}

// 加载详情数据
const loadDetail = async () => {
  const id = route.query.id
  if (!id) {
    ElMessage.error('缺少流程ID')
    return
  }

  loading.value = true
  try {
    const res = await getWorkflowInstance(id)
    if (res && res.success) {
      instance.value = res.data
      businessData.value = res.data.businessData ? JSON.parse(res.data.businessData) : null
      // TODO: 加载审批记录
    } else {
      ElMessage.error(res?.message || '获取详情失败')
    }
  } catch (error) {
    console.error('获取详情失败:', error)
    ElMessage.error('获取详情失败')
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  loadDetail()
})
</script>

<style scoped>
.workflow-detail {
  padding: 20px;
}

.detail-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.detail-header .title {
  font-size: 18px;
  font-weight: bold;
}

.detail-content {
  margin-top: 20px;
}

.business-data,
.approval-records {
  margin-top: 30px;
}

.business-data h3,
.approval-records h3 {
  margin-bottom: 15px;
  font-size: 16px;
  font-weight: bold;
  color: #303133;
}
</style>
