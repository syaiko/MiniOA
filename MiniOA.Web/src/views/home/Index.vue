<template>
  <div class="dashboard-container">
    <!-- ========== 个人数据（所有角色可见） ========== -->
    <el-row :gutter="20">
      <el-col :span="6">
        <el-card class="stat-card">
          <div class="stat-item">
            <div class="stat-label">我的待办</div>
            <div class="stat-value primary">{{ dashboard.myPendingTasks || 0 }}</div>
          </div>
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card class="stat-card">
          <div class="stat-item">
            <div class="stat-label">已完成</div>
            <div class="stat-value success">{{ dashboard.myCompletedTasks || 0 }}</div>
          </div>
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card class="stat-card">
          <div class="stat-item">
            <div class="stat-label">我的申请</div>
            <div class="stat-value">{{ dashboard.myApplications || 0 }}</div>
          </div>
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card class="stat-card">
          <div class="stat-item">
            <div class="stat-label">未读消息</div>
            <div class="stat-value warning">{{ dashboard.myUnreadNotifications || 0 }}</div>
          </div>
        </el-card>
      </el-col>
    </el-row>

    <!-- ========== 部门数据（部门经理及以上可见） ========== -->
    <template v-if="dashboard.showDepartmentData">
      <el-divider content-position="left">
        <el-icon><OfficeBuilding /></el-icon>
        {{ dashboard.departmentName }} - 部门数据
      </el-divider>

      <el-row :gutter="20">
        <el-col :span="6">
          <el-card class="stat-card">
            <div class="stat-item">
              <div class="stat-label">部门待办</div>
              <div class="stat-value primary">{{ dashboard.departmentPendingTasks || 0 }}</div>
            </div>
          </el-card>
        </el-col>
        <el-col :span="6">
          <el-card class="stat-card">
            <div class="stat-item">
              <div class="stat-label">部门已完成</div>
              <div class="stat-value success">{{ dashboard.departmentCompletedTasks || 0 }}</div>
            </div>
          </el-card>
        </el-col>
        <el-col :span="6">
          <el-card class="stat-card">
            <div class="stat-item">
              <div class="stat-label">部门成员</div>
              <div class="stat-value">{{ dashboard.departmentMemberCount || 0 }}</div>
            </div>
          </el-card>
        </el-col>
        <el-col :span="6">
          <el-card class="stat-card">
            <div class="stat-item">
              <div class="stat-label">部门完成率</div>
              <div class="stat-value" :class="getCompletionClass(dashboard.departmentCompletionRate)">
                {{ dashboard.departmentCompletionRate || 0 }}%
              </div>
            </div>
          </el-card>
        </el-col>
      </el-row>

      <el-row :gutter="20" style="margin-top: 20px;">
        <el-col :span="12">
          <el-card>
            <template #header>
              <span>部门成员任务完成率</span>
            </template>
            <div ref="deptMemberChartRef" style="height: 300px;"></div>
          </el-card>
        </el-col>
        <el-col :span="12">
          <el-card>
            <template #header>
              <span>部门任务趋势（近6个月）</span>
            </template>
            <div ref="deptTrendChartRef" style="height: 300px;"></div>
          </el-card>
        </el-col>
      </el-row>
    </template>

    <!-- ========== 全局数据（管理员/超级管理员可见） ========== -->
    <template v-if="dashboard.showGlobalData">
      <el-divider content-position="left">
        <el-icon><DataAnalysis /></el-icon>
        全局数据
      </el-divider>

      <el-row :gutter="20">
        <el-col :span="4">
          <el-card class="stat-card">
            <div class="stat-item">
              <div class="stat-label">总任务数</div>
              <div class="stat-value">{{ dashboard.globalTotalTasks || 0 }}</div>
            </div>
          </el-card>
        </el-col>
        <el-col :span="4">
          <el-card class="stat-card">
            <div class="stat-item">
              <div class="stat-label">待办任务</div>
              <div class="stat-value primary">{{ dashboard.globalPendingTasks || 0 }}</div>
            </div>
          </el-card>
        </el-col>
        <el-col :span="4">
          <el-card class="stat-card">
            <div class="stat-item">
              <div class="stat-label">已完成</div>
              <div class="stat-value success">{{ dashboard.globalCompletedTasks || 0 }}</div>
            </div>
          </el-card>
        </el-col>
        <el-col :span="4">
          <el-card class="stat-card">
            <div class="stat-item">
              <div class="stat-label">逾期任务</div>
              <div class="stat-value danger">{{ dashboard.globalOverdueTasks || 0 }}</div>
            </div>
          </el-card>
        </el-col>
        <el-col :span="4">
          <el-card class="stat-card">
            <div class="stat-item">
              <div class="stat-label">完成率</div>
              <div class="stat-value" :class="getCompletionClass(dashboard.globalCompletionRate)">
                {{ dashboard.globalCompletionRate || 0 }}%
              </div>
            </div>
          </el-card>
        </el-col>
        <el-col :span="4">
          <el-card class="stat-card">
            <div class="stat-item">
              <div class="stat-label">逾期率</div>
              <div class="stat-value danger">{{ dashboard.globalOverdueRate || 0 }}%</div>
            </div>
          </el-card>
        </el-col>
      </el-row>

      <el-row :gutter="20" style="margin-top: 20px;">
        <el-col :span="24">
          <el-card>
            <template #header>
              <span>全公司任务趋势（近12个月）</span>
            </template>
            <div ref="globalTrendChartRef" style="height: 350px;"></div>
          </el-card>
        </el-col>
      </el-row>

      <el-row :gutter="20" style="margin-top: 20px;">
        <el-col :span="24">
          <el-card>
            <template #header>
              <span>各部门任务统计</span>
            </template>
            <el-table :data="dashboard.departmentStats || []" border style="width: 100%">
              <el-table-column prop="departmentName" label="部门名称" width="150" />
              <el-table-column prop="memberCount" label="成员数" width="100" />
              <el-table-column prop="totalTasks" label="任务总数" width="100" />
              <el-table-column prop="pendingTasks" label="待办" width="100" />
              <el-table-column prop="completedTasks" label="已完成" width="100" />
              <el-table-column prop="overdueTasks" label="逾期" width="100">
                <template #default="{ row }">
                  <el-tag v-if="row.overdueTasks > 0" type="danger">{{ row.overdueTasks }}</el-tag>
                  <span v-else>0</span>
                </template>
              </el-table-column>
              <el-table-column prop="completionRate" label="完成率">
                <template #default="{ row }">
                  <el-progress :percentage="row.completionRate" :color="getProgressColor(row.completionRate)" />
                </template>
              </el-table-column>
            </el-table>
          </el-card>
        </el-col>
      </el-row>
    </template>

    <!-- ========== 快速操作 + 最近动态 ========== -->
    <el-divider content-position="left">
      <el-icon><Opportunity /></el-icon>
      快速操作
    </el-divider>

    <el-row :gutter="20">
      <el-col :span="12">
        <el-card>
          <template #header>
            <span>快速操作</span>
          </template>
          <div class="quick-actions">
            <el-button type="primary" size="large" @click="$router.push('/approval/submit')">
              <el-icon><Document /></el-icon>
              发起申请
            </el-button>
            <el-button type="success" size="large" @click="$router.push('/approval/pending')">
              <el-icon><Clock /></el-icon>
              待我审批
            </el-button>
            <el-button type="info" size="large" @click="$router.push('/tasks')">
              <el-icon><List /></el-icon>
              任务管理
            </el-button>
            <el-button type="warning" size="large" @click="$router.push('/notification/unread')">
              <el-icon><Bell /></el-icon>
              消息中心
            </el-button>
          </div>
        </el-card>
      </el-col>

      <el-col :span="12">
        <el-card>
          <template #header>
            <span>最近动态</span>
          </template>
          <div class="recent-activities">
            <div class="activity-item" v-for="item in dashboard.recentActivities" :key="item.id">
              <div class="activity-icon">
                <el-icon><Bell /></el-icon>
              </div>
              <div class="activity-content">
                <div class="activity-title">{{ item.title }}</div>
                <div class="activity-time">{{ item.time }}</div>
              </div>
            </div>
            <div v-if="!dashboard.recentActivities?.length" class="no-activities">
              暂无动态
            </div>
          </div>
        </el-card>
      </el-col>
    </el-row>
  </div>
</template>

<script setup>
import { ref, onMounted, onBeforeUnmount, nextTick, watch } from 'vue'
import { Document, Clock, List, Bell, OfficeBuilding, DataAnalysis, Opportunity } from '@element-plus/icons-vue'
import { getDashboard } from '../../api/task'
import { ElMessage } from 'element-plus'
import * as echarts from 'echarts'

const dashboard = ref({
  myPendingTasks: 0,
  myCompletedTasks: 0,
  myApplications: 0,
  myUnreadNotifications: 0,
  myCompletionRate: 0,
  showDepartmentData: false,
  showGlobalData: false
})

const deptMemberChartRef = ref(null)
const deptTrendChartRef = ref(null)
const globalTrendChartRef = ref(null)

let deptMemberChart = null
let deptTrendChart = null
let globalTrendChart = null

onMounted(() => {
  loadDashboardData()
  window.addEventListener('resize', handleResize)
})

onBeforeUnmount(() => {
  window.removeEventListener('resize', handleResize)
  deptMemberChart?.dispose()
  deptTrendChart?.dispose()
  globalTrendChart?.dispose()
})

const loadDashboardData = async () => {
  try {
    const res = await getDashboard()
    if (res && res.success) {
      dashboard.value = res.data
      await nextTick()
      renderCharts()
    }
  } catch {
    ElMessage.error('加载仪表盘数据失败')
  }
}

const renderCharts = () => {
  if (dashboard.value.showDepartmentData) {
    renderDeptMemberChart()
    renderDeptTrendChart()
  }
  if (dashboard.value.showGlobalData) {
    renderGlobalTrendChart()
  }
}

// 部门成员完成率柱状图
const renderDeptMemberChart = () => {
  if (!deptMemberChartRef.value) return
  deptMemberChart = echarts.init(deptMemberChartRef.value)

  const members = dashboard.value.departmentMembers || []
  const names = members.map(m => m.userName)
  const rates = members.map(m => m.completionRate)

  const option = {
    tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' } },
    grid: { left: '3%', right: '4%', bottom: '3%', containLabel: true },
    xAxis: {
      type: 'category',
      data: names,
      axisLabel: { interval: 0, rotate: 30 }
    },
    yAxis: {
      type: 'value',
      max: 100,
      axisLabel: { formatter: '{value}%' }
    },
    series: [{
      name: '完成率',
      type: 'bar',
      data: rates,
      itemStyle: {
        color: (params) => {
          const val = params.value
          if (val >= 80) return '#67C23A'
          if (val >= 60) return '#E6A23C'
          return '#F56C6C'
        }
      }
    }]
  }

  deptMemberChart.setOption(option)
}

// 部门任务趋势折线图
const renderDeptTrendChart = () => {
  if (!deptTrendChartRef.value) return
  deptTrendChart = echarts.init(deptTrendChartRef.value)

  const trend = dashboard.value.departmentTrend || []
  const months = trend.map(t => t.monthLabel)
  const created = trend.map(t => t.createdCount)
  const completed = trend.map(t => t.completedCount)

  const option = {
    tooltip: { trigger: 'axis' },
    legend: { data: ['创建任务', '完成任务'] },
    grid: { left: '3%', right: '4%', bottom: '3%', containLabel: true },
    xAxis: { type: 'category', boundaryGap: false, data: months },
    yAxis: { type: 'value' },
    series: [
      {
        name: '创建任务',
        type: 'line',
        data: created,
        smooth: true,
        itemStyle: { color: '#409EFF' }
      },
      {
        name: '完成任务',
        type: 'line',
        data: completed,
        smooth: true,
        itemStyle: { color: '#67C23A' }
      }
    ]
  }

  deptTrendChart.setOption(option)
}

// 全公司任务趋势折线图
const renderGlobalTrendChart = () => {
  if (!globalTrendChartRef.value) return
  globalTrendChart = echarts.init(globalTrendChartRef.value)

  const trend = dashboard.value.globalTrend || []
  const months = trend.map(t => t.monthLabel)
  const created = trend.map(t => t.createdCount)
  const completed = trend.map(t => t.completedCount)
  const overdue = trend.map(t => t.overdueCount)

  const option = {
    tooltip: { trigger: 'axis', axisPointer: { type: 'cross' } },
    legend: { data: ['创建任务', '完成任务', '逾期任务'] },
    grid: { left: '3%', right: '3%', bottom: '10%', containLabel: true },
    xAxis: { type: 'category', boundaryGap: false, data: months },
    yAxis: { type: 'value' },
    dataZoom: [
      { type: 'inside', start: 0, end: 100 },
      { type: 'slider', height: 18, bottom: 0, start: 0, end: 100 }
    ],
    series: [
      {
        name: '创建任务',
        type: 'line',
        data: created,
        smooth: true,
        symbol: 'circle',
        symbolSize: 6,
        itemStyle: { color: '#409EFF' },
        lineStyle: { width: 3 }
      },
      {
        name: '完成任务',
        type: 'line',
        data: completed,
        smooth: true,
        symbol: 'circle',
        symbolSize: 6,
        itemStyle: { color: '#67C23A' },
        lineStyle: { width: 3 }
      },
      {
        name: '逾期任务',
        type: 'line',
        data: overdue,
        smooth: true,
        symbol: 'circle',
        symbolSize: 6,
        itemStyle: { color: '#F56C6C' },
        lineStyle: { width: 3 }
      }
    ]
  }

  globalTrendChart.setOption(option)
}

const handleResize = () => {
  deptMemberChart?.resize()
  deptTrendChart?.resize()
  globalTrendChart?.resize()
}

const getCompletionClass = (rate) => {
  if (rate >= 80) return 'success'
  if (rate >= 60) return 'warning'
  return 'danger'
}

const getProgressColor = (rate) => {
  if (rate >= 80) return '#67C23A'
  if (rate >= 60) return '#E6A23C'
  return '#F56C6C'
}
</script>

<style scoped>
.dashboard-container {
  padding: 20px;
}

.stat-card {
  text-align: center;
}

.stat-item {
  padding: 15px 0;
}

.stat-label {
  font-size: 14px;
  color: #909399;
  margin-bottom: 10px;
}

.stat-value {
  font-size: 28px;
  font-weight: bold;
  color: #303133;
}

.stat-value.primary {
  color: #409EFF;
}

.stat-value.success {
  color: #67C23A;
}

.stat-value.warning {
  color: #E6A23C;
}

.stat-value.danger {
  color: #F56C6C;
}

.quick-actions {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 16px;
}

.quick-actions .el-button {
  height: 60px;
  font-size: 16px;
}

.quick-actions .el-icon {
  margin-right: 8px;
}

.recent-activities {
  max-height: 300px;
  overflow-y: auto;
}

.activity-item {
  display: flex;
  align-items: center;
  padding: 12px 0;
  border-bottom: 1px solid #f0f0f0;
}

.activity-item:last-child {
  border-bottom: none;
}

.activity-icon {
  margin-right: 12px;
  color: #409EFF;
}

.activity-content {
  flex: 1;
}

.activity-title {
  font-size: 14px;
  color: #303133;
  margin-bottom: 4px;
}

.activity-time {
  font-size: 12px;
  color: #909399;
}

.no-activities {
  text-align: center;
  color: #909399;
  padding: 40px 0;
}

.el-divider {
  margin: 30px 0 20px;
}

.el-divider__text {
  font-size: 16px;
  font-weight: 600;
  color: #303133;
}
</style>
