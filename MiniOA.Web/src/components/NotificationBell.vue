<template>
  <el-badge :value="unreadCount" :max="99" :hidden="unreadCount === 0">
    <el-icon class="notification-icon" @click="handleClick">
      <Bell />
    </el-icon>
  </el-badge>
</template>

<script setup>
import { ref, onMounted, onUnmounted, watch } from 'vue'
import { useRouter } from 'vue-router'
import { Bell } from '@element-plus/icons-vue'
import { useUserStore } from '../store/user'
import notificationService from '../utils/notification'
import { getUnreadCount } from '../api/notification'

const router = useRouter()
const userStore = useUserStore()
const unreadCount = ref(0)

let pollTimer = null

// 点击通知图标
const handleClick = () => {
  router.push('/notification/unread')
}

// 获取未读通知数量
const fetchUnreadCount = async () => {
  try {
    const res = await getUnreadCount()
    if (res.data && res.data.count !== undefined) {
      unreadCount.value = res.data.count
    }
  } catch (error) {
    console.error('获取未读数量失败:', error)
  }
}

// 监听通知事件
const handleNotificationReceived = async () => {
  await fetchUnreadCount()
}

// 监听标记已读事件
const handleNotificationMarkRead = async () => {
  await fetchUnreadCount()
}

// 监听全部已读事件
const handleNotificationMarkAllRead = async () => {
  await fetchUnreadCount()
}

const startPolling = () => {
  if (pollTimer) return
  pollTimer = setInterval(fetchUnreadCount, 15000)
}

const stopPolling = () => {
  if (pollTimer) {
    clearInterval(pollTimer)
    pollTimer = null
  }
}

const tryConnect = async (token) => {
  if (!token) return
  await notificationService.connect(token)
}

onMounted(async () => {
  // 获取初始未读数量
  await fetchUnreadCount()

  // 监听通知事件
  window.addEventListener('notification-received', handleNotificationReceived)
  window.addEventListener('notification-mark-read', handleNotificationMarkRead)
  window.addEventListener('notification-mark-all-read', handleNotificationMarkAllRead)

  // 启动轮询兜底（SignalR异常时仍能更新）
  startPolling()

  // 如果mounted时已经有token，直接连接
  if (userStore.token) {
    await tryConnect(userStore.token)
  }
})

watch(
  () => userStore.token,
  async (newToken, oldToken) => {
    // 解决mounted时token未就绪导致无法连接SignalR
    if (newToken && newToken !== oldToken) {
      await tryConnect(newToken)
    }

    if (!newToken && oldToken) {
      await notificationService.disconnect()
    }
  }
)

onUnmounted(async () => {
  stopPolling()

  // 断开连接
  await notificationService.disconnect()
  
  // 移除监听
  window.removeEventListener('notification-received', handleNotificationReceived)
  window.removeEventListener('notification-mark-read', handleNotificationMarkRead)
  window.removeEventListener('notification-mark-all-read', handleNotificationMarkAllRead)
})

// 暴露方法供父组件调用
defineExpose({
  clearUnread: () => {
    unreadCount.value = 0
  },
  refreshUnreadCount: fetchUnreadCount
})
</script>

<style scoped>
.notification-icon {
  font-size: 20px;
  cursor: pointer;
  color: #606266;
}

.notification-icon:hover {
  color: #409EFF;
}
</style>
