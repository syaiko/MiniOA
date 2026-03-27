import * as signalR from '@microsoft/signalr'
import { ElNotification } from 'element-plus'

// 获取API基础URL（与request.js保持一致）
const getBaseURL = () => {
  // 统一使用线上地址，确保线上部署后功能正常
  return 'https://familysys.hejiancheng.xyz'
}

class NotificationService {
  constructor() {
    this.connection = null
    this.isConnected = false
  }

  // 连接SignalR
  async connect(token) {
    if (this.isConnected) return

    try {
      const baseUrl = getBaseURL()
      this.connection = new signalR.HubConnectionBuilder()
        .withUrl(`${baseUrl}/notificationHub`, {
          accessTokenFactory: () => token,
          // 强制使用 LongPolling 降级，如果 WebSocket 被代理拦截
          transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling
        })
        .withAutomaticReconnect()
        .build()

      // 接收消息
      this.connection.on('ReceiveNotification', (message) => {
        this.handleNotification(message)
      })

      // 连接事件
      this.connection.onreconnecting(() => {
        console.log('SignalR 重连中...')
        this.isConnected = false
      })

      this.connection.onreconnected(() => {
        console.log('SignalR 重连成功')
        this.isConnected = true
      })

      this.connection.onclose(() => {
        console.log('SignalR 连接关闭')
        this.isConnected = false
      })

      await this.connection.start()
      this.isConnected = true
      console.log('SignalR 连接成功')
    } catch (error) {
      console.error('SignalR 连接失败:', error)
    }
  }

  // 断开连接
  async disconnect() {
    if (this.connection) {
      await this.connection.stop()
      this.connection = null
      this.isConnected = false
    }
  }

  // 处理通知
  handleNotification(message) {
    // 解析消息
    let notification
    try {
      notification = typeof message === 'string' ? JSON.parse(message) : message
    } catch {
      notification = { title: '通知', content: message }
    }

    // 弹出通知
    ElNotification({
      title: notification.title || '新消息',
      message: notification.content || message,
      type: notification.type || 'info',
      duration: 4500,
      position: 'top-right'
    })

    // 触发自定义事件
    window.dispatchEvent(new CustomEvent('notification-received', {
      detail: notification
    }))
  }

  // 发送消息给指定用户
  async sendToUser(userId, message) {
    if (!this.connection || !this.isConnected) {
      console.error('SignalR 未连接')
      return false
    }

    try {
      await this.connection.invoke('SendToUser', userId, message)
      return true
    } catch (error) {
      console.error('发送消息失败:', error)
      return false
    }
  }
}

export default new NotificationService()
