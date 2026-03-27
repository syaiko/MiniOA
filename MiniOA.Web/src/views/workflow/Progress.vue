<template>
  <div class="workflow-progress">
    <el-card>
      <template #header>
        <span class="card-header">审批进度</span>
      </template>

      <el-steps :active="activeStep" align-center>
        <el-step
          v-for="node in nodes"
          :key="node.id"
          :title="node.name"
          :description="getNodeDescription(node)"
          :status="getNodeStatus(node)"
        />
      </el-steps>

      <el-divider />

      <!-- 审批记录 -->
      <div class="approval-records" v-if="records.length > 0">
        <h4>审批记录</h4>
        <el-timeline>
          <el-timeline-item
            v-for="record in records"
            :key="record.id"
            :timestamp="formatDate(record.createdAt)"
            placement="top"
          >
            <el-card>
              <div class="record-content">
                <div class="record-header">
                  <span class="node-name">{{ record.nodeName }}</span>
                  <el-tag :type="getActionTagType(record.action)" size="small">
                    {{ getActionName(record.action) }}
                  </el-tag>
                </div>
                <div class="record-body">
                  <p><strong>审批人：</strong>{{ record.approverName }}</p>
                  <p v-if="record.comment"><strong>意见：</strong>{{ record.comment }}</p>
                </div>
              </div>
            </el-card>
          </el-timeline-item>
        </el-timeline>
      </div>
    </el-card>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue'
import { useRoute } from 'vue-router'
import { getWorkflowNodes, getWorkflowInstance } from '../../api/workflow'

const route = useRoute()
const instanceId = ref(route.params.id || route.query.id)

const nodes = ref([])
const records = ref([])
const currentInstanceId = ref(null)
const currentNodeId = ref(null)
const workflowStatus = ref(null)

// 当前激活步骤
const activeStep = computed(() => {
  if (!currentNodeId.value || nodes.value.length === 0) return 0
  
  const index = nodes.value.findIndex(n => n.id === currentNodeId.value)
  return index >= 0 ? index : 0
})

// 获取节点描述
const getNodeDescription = (node) => {
  if (node.approverName) {
    return `审批人: ${node.approverName}`
  }
  if (node.departmentName) {
    return `部门: ${node.departmentName}`
  }
  return ''
}

// 获取节点状态
const getNodeStatus = (node) => {
  if (!currentNodeId.value) return 'wait'
  
  const currentIndex = nodes.value.findIndex(n => n.id === currentNodeId.value)
  const nodeIndex = nodes.value.findIndex(n => n.id === node.id)
  
  if (workflowStatus.value === 'Approved') {
    return 'success'
  }
  
  if (workflowStatus.value === 'Rejected' && nodeIndex === currentIndex) {
    return 'error'
  }
  
  if (nodeIndex < currentIndex) {
    return 'success'
  }
  if (nodeIndex === currentIndex) {
    return 'process'
  }
  return 'wait'
}

// 获取动作标签类型
const getActionTagType = (action) => {
  const types = {
    'Submit': 'info',
    'Approve': 'success',
    'Reject': 'danger',
    'Return': 'warning',
    'Cancel': 'info'
  }
  return types[action] || 'info'
}

// 获取动作名称
const getActionName = (action) => {
  const names = {
    'Submit': '提交',
    'Approve': '批准',
    'Reject': '拒绝',
    'Return': '退回',
    'Cancel': '取消'
  }
  return names[action] || action
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

// 加载数据
const loadData = async () => {
  if (!instanceId.value) return

  try {
    // 获取流程实例
    const instanceRes = await getWorkflowInstance(instanceId.value)
    if (instanceRes.data.success) {
      const instance = instanceRes.data.data
      currentInstanceId.value = instance.id
      currentNodeId.value = instance.currentNodeId
      workflowStatus.value = instance.status
      
      // 获取流程节点
      const nodesRes = await getWorkflowNodes(instance.workflowType)
      if (nodesRes.data.success) {
        nodes.value = nodesRes.data.data
      }
      
      // 获取审批记录（需要从实例详情中获取）
      // 这里假设实例详情包含审批记录，如果没有需要单独接口
      records.value = instance.approvalRecords || []
    }
  } catch (error) {
    console.error('加载数据失败:', error)
  }
}

// 监听路由参数变化
watch(() => route.params.id, (newId) => {
  if (newId) {
    instanceId.value = newId
    loadData()
  }
})

onMounted(() => {
  loadData()
})

// 暴露方法供父组件调用
defineExpose({
  loadData,
  instanceId
})
</script>

<style scoped>
.workflow-progress {
  padding: 20px;
}

.card-header {
  font-size: 18px;
  font-weight: bold;
}

.el-steps {
  margin-bottom: 20px;
}

.approval-records h4 {
  margin-bottom: 20px;
  color: #303133;
}

.record-content {
  padding: 10px;
}

.record-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 10px;
}

.node-name {
  font-weight: bold;
  font-size: 16px;
}

.record-body p {
  margin: 5px 0;
  color: #606266;
}
</style>
