<template>
  <div class="approval-done">
    <el-card>
      <template #header>
        <span class="card-header">已办事项</span>
      </template>

      <el-table :data="doneList" v-loading="loading" style="width: 100%">
        <el-table-column prop="title" label="标题" min-width="150" />
        <el-table-column prop="workflowType" label="流程类型" width="120">
          <template #default="{ row }">
            <el-tag :type="getWorkflowTypeTag(row.workflowType)" size="small">
              {{ getWorkflowTypeName(row.workflowType) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="creatorName" label="申请人" width="120" />
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
        <el-table-column label="操作" width="100" fixed="right">
          <template #default="{ row }">
            <el-button size="small" @click="handleViewDetail(row)">详情</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { getMyDone } from '../../api/workflow'

const router = useRouter()
const loading = ref(false)
const doneList = ref([])

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
    0: 'info',
    1: 'warning',
    2: 'success',
    3: 'danger'
  }
  return types[status] || 'info'
}

const getStatusName = (status) => {
  const names = {
    0: '待审批',
    1: '审批中',
    2: '已批准',
    3: '已拒绝'
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

const loadDoneList = async () => {
  loading.value = true
  try {
    const res = await getMyDone()
    if (res && res.success) {
      doneList.value = res.data || []
    }
  } catch (error) {
    ElMessage.error('加载已办列表失败')
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  loadDoneList()
})
</script>

<style scoped>
.approval-done {
  padding: 20px;
}

.card-header {
  font-size: 18px;
  font-weight: bold;
}
</style>
