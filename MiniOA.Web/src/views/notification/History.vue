<template>
  <div class="notification-history">
    <el-card>
      <template #header>
        <span class="card-header">历史消息</span>
      </template>

      <el-table :data="notifications" v-loading="loading" style="width: 100%">
        <el-table-column prop="title" label="标题" min-width="200" />
        <el-table-column prop="content" label="内容" min-width="300">
          <template #default="{ row }">
            <div class="notification-content">{{ row.content }}</div>
          </template>
        </el-table-column>
        <el-table-column prop="type" label="类型" width="120">
          <template #default="{ row }">
            <el-tag :type="getTypeTagType(row.type)" size="small">
              {{ getTypeName(row.type) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="createdAt" label="时间" width="180">
          <template #default="{ row }">
            {{ formatDate(row.createdAt) }}
          </template>
        </el-table-column>
        <el-table-column prop="readAt" label="阅读时间" width="180">
          <template #default="{ row }">
            {{ row.readAt ? formatDate(row.readAt) : '-' }}
          </template>
        </el-table-column>
        <el-table-column label="操作" width="150" fixed="right">
          <template #default="{ row }">
            <el-button size="small" @click="handleView(row)">查看</el-button>
            <el-button size="small" type="success" @click="handleMarkRead(row)" v-if="!row.isRead">标为已读</el-button>
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
import { getNotifications, markAsRead } from '../../api/notification'

const router = useRouter()
const loading = ref(false)
const notifications = ref([])

const getTypeTagType = (type) => {
  const types = {
    'Workflow': 'primary',
    'Task': 'success',
    'System': 'info',
    'Reminder': 'warning',
    'WorkflowCc': ''
  }
  return types[type] || 'info'
}

const getTypeName = (type) => {
  const names = {
    'Workflow': '审批通知',
    'Task': '任务通知',
    'System': '系统通知',
    'Reminder': '提醒通知',
    'WorkflowCc': '抄送通知'
  }
  return names[type] || type
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

const handleView = (row) => {
  // 根据通知类型跳转到不同页面
  if (row.type === 'Task' && row.relatedId) {
    // 任务通知 -> 跳转到任务管理页面
    router.push('/tasks')
  } else if ((row.type === 'Workflow' || row.type === 'WorkflowCc') && row.relatedId) {
    // 工作流通知 -> 跳转到待我审批页面
    router.push('/approval/pending')
  }
  
  // 自动标记为已读
  handleMarkRead(row, true)
}

const handleMarkRead = async (row, silent = false) => {
  try {
    await markAsRead(row.id)
    // 更新列表中的状态
    const index = notifications.value.findIndex(n => n.id === row.id)
    if (index !== -1) {
      notifications.value[index].isRead = true
      notifications.value[index].readAt = new Date().toISOString()
    }
    if (!silent) {
      ElMessage.success('已标记为已读')
    }
    
    // 通知父组件刷新未读数量
    window.dispatchEvent(new CustomEvent('notification-mark-read'))
  } catch (error) {
    if (!silent) {
      ElMessage.error('标记失败')
    }
  }
}

const loadNotifications = async () => {
  loading.value = true
  try {
    const res = await getNotifications({ isRead: true, pageSize: 100 })
    notifications.value = res.data.notifications || []
  } catch (error) {
    ElMessage.error('加载消息列表失败')
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  loadNotifications()
})
</script>

<style scoped>
.notification-history {
  padding: 20px;
}

.card-header {
  font-size: 18px;
  font-weight: bold;
}

.notification-content {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
</style>
