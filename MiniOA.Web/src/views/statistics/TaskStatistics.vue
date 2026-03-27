<template>
  <div class="task-statistics">
    <el-row :gutter="20">
      <!-- 概览卡片 -->
      <el-col :span="6">
        <el-card class="stat-card">
          <div class="stat-item">
            <div class="stat-label">本月任务总数</div>
            <div class="stat-value">{{ statistics.monthlyTotalCount || 0 }}</div>
          </div>
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card class="stat-card">
          <div class="stat-item">
            <div class="stat-label">本月已完成</div>
            <div class="stat-value success">{{ statistics.monthlyCompletedCount || 0 }}</div>
          </div>
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card class="stat-card">
          <div class="stat-item">
            <div class="stat-label">完成率</div>
            <div class="stat-value primary">{{ statistics.monthlyCompletionRate || 0 }}%</div>
          </div>
        </el-card>
      </el-col>
      <el-col :span="6">
        <el-card class="stat-card">
          <div class="stat-item">
            <div class="stat-label">逾期率</div>
            <div class="stat-value danger">{{ statistics.overdueRate || 0 }}%</div>
          </div>
        </el-card>
      </el-col>
    </el-row>

    <!-- 主图：任务趋势 -->
    <el-row :gutter="20" style="margin-top: 20px;">
      <el-col :span="24">
        <el-card class="chart-card">
          <template #header>
            <span>任务趋势（12个月）</span>
          </template>
          <div ref="lineChartRef" class="chart chart-lg"></div>
        </el-card>
      </el-col>
    </el-row>

    <!-- 部门任务分布 -->
    <el-row :gutter="20" style="margin-top: 20px;">
      <el-col :span="24">
        <el-card class="chart-card">
          <template #header>
            <span>部门任务分布</span>
          </template>
          <div ref="pieChartRef" class="chart chart-md"></div>
        </el-card>
      </el-col>
    </el-row>

    <!-- 部门完成率对比 -->
    <el-row :gutter="20" style="margin-top: 20px;">
      <el-col :span="24">
        <el-card class="chart-card">
          <template #header>
            <span>部门完成率对比</span>
          </template>
          <div ref="barChartRef" class="chart chart-md"></div>
        </el-card>
      </el-col>
    </el-row>

    <el-row :gutter="20" style="margin-top: 20px;">
      <!-- 部门饱和度表格 -->
      <el-col :span="24">
        <el-card>
          <template #header>
            <span>各部门任务饱和度明细</span>
          </template>
          <el-table :data="statistics.departmentSaturation || []" border style="width: 100%">
            <el-table-column prop="departmentName" label="部门名称" width="150" />
            <el-table-column prop="totalMembers" label="部门人数" width="100" />
            <el-table-column prop="taskCount" label="任务总数" width="100" />
            <el-table-column prop="completedCount" label="已完成" width="100" />
            <el-table-column prop="inProgressCount" label="进行中" width="100" />
            <el-table-column prop="overdueCount" label="逾期数" width="100">
              <template #default="{ row }">
                <el-tag v-if="row.overdueCount > 0" type="danger">{{ row.overdueCount }}</el-tag>
                <span v-else>0</span>
              </template>
            </el-table-column>
            <el-table-column prop="saturationRate" label="饱和度（人均）" width="120">
              <template #default="{ row }">
                <el-progress :percentage="Math.min(row.saturationRate * 20, 100)" :color="getSaturationColor(row.saturationRate)" />
                <span style="font-size: 12px;">{{ row.saturationRate }} 任务/人</span>
              </template>
            </el-table-column>
            <el-table-column prop="completionRate" label="完成率">
              <template #default="{ row }">
                <el-progress :percentage="row.completionRate" :color="getCompletionColor(row.completionRate)" />
              </template>
            </el-table-column>
          </el-table>
        </el-card>
      </el-col>
    </el-row>
  </div>
</template>

<script setup>
import { ref, onMounted, onBeforeUnmount, nextTick } from 'vue'
import { ElMessage } from 'element-plus'
import * as echarts from 'echarts'
import { getTaskStatistics, getTaskTrend } from '../../api/task'

const statistics = ref({
  monthlyTotalCount: 0,
  monthlyCompletedCount: 0,
  monthlyCompletionRate: 0,
  overdueCount: 0,
  overdueRate: 0,
  departmentSaturation: []
})

const trendData = ref({
  trendData: []
})

const pieChartRef = ref(null)
const barChartRef = ref(null)
const lineChartRef = ref(null)
let pieChart = null
let barChart = null
let lineChart = null

const safeInit = (chart, el) => {
  if (!el) return chart
  if (chart) return chart
  return echarts.init(el)
}

// 加载统计数据
const loadStatistics = async () => {
  try {
    const res = await getTaskStatistics()
    if (res && res.success) {
      statistics.value = res.data || {}
    }
  } catch (error) {
    ElMessage.error('加载统计数据失败')
  }
}

// 加载趋势数据
const loadTrendData = async () => {
  try {
    const res = await getTaskTrend()
    if (res && res.success) {
      trendData.value = res.data || {}
    }
  } catch (error) {
    ElMessage.error('加载趋势数据失败')
  }
}

// 加载所有数据
const loadAllData = async () => {
  await Promise.all([loadStatistics(), loadTrendData()])
  await nextTick()
  renderCharts()
}

// 渲染图表
const renderCharts = () => {
  renderPieChart()
  renderBarChart()
  renderLineChart()
}

// 饼图：部门任务分布
const renderPieChart = () => {
  if (!pieChartRef.value) return

  pieChart = safeInit(pieChart, pieChartRef.value)

  const data = statistics.value.departmentSaturation.map(item => ({
    name: item.departmentName,
    value: item.taskCount
  }))

  const option = {
    tooltip: {
      trigger: 'item',
      formatter: '{b}: {c} ({d}%)'
    },
    legend: {
      type: 'scroll',
      bottom: 0,
      left: 'center'
    },
    series: [
      {
        name: '任务分布',
        type: 'pie',
        radius: ['35%', '70%'],
        center: ['50%', '45%'],
        avoidLabelOverlap: false,
        itemStyle: {
          borderRadius: 10,
          borderColor: '#fff',
          borderWidth: 2
        },
        label: {
          show: true,
          formatter: '{b}: {c}'
        },
        emphasis: {
          label: {
            show: true,
            fontSize: 16,
            fontWeight: 'bold'
          }
        },
        data: data
      }
    ]
  }

  pieChart.setOption(option, true)
}

// 柱状图：部门完成率对比
const renderBarChart = () => {
  if (!barChartRef.value) return

  barChart = safeInit(barChart, barChartRef.value)

  const departments = statistics.value.departmentSaturation.map(item => item.departmentName)
  const completionRates = statistics.value.departmentSaturation.map(item => item.completionRate)
  const overdueRates = statistics.value.departmentSaturation.map(item => {
    const total = item.taskCount || 1
    return Number(((item.overdueCount / total) * 100).toFixed(2))
  })

  const option = {
    tooltip: {
      trigger: 'axis',
      axisPointer: {
        type: 'shadow'
      }
    },
    legend: {
      data: ['完成率', '逾期率']
    },
    grid: {
      left: '4%',
      right: '4%',
      bottom: 40,
      containLabel: true
    },
    xAxis: {
      type: 'category',
      data: departments,
      axisLabel: {
        interval: 0,
        rotate: 0,
        formatter: (value) => {
          if (!value) return ''
          // 每个字符换行显示，实现竖排效果
          return value.split('').join('\n')
        }
      }
    },
    yAxis: {
      type: 'value',
      max: 100,
      axisLabel: {
        formatter: '{value}%'
      }
    },
    series: [
      {
        name: '完成率',
        type: 'bar',
        data: completionRates,
        barMaxWidth: 28,
        itemStyle: {
          color: '#67C23A'
        }
      },
      {
        name: '逾期率',
        type: 'bar',
        data: overdueRates,
        barMaxWidth: 28,
        itemStyle: {
          color: '#F56C6C'
        }
      }
    ]
  }

  barChart.setOption(option, true)
}

// 折线图：任务趋势
const renderLineChart = () => {
  if (!lineChartRef.value) return

  lineChart = safeInit(lineChart, lineChartRef.value)

  const months = trendData.value.trendData.map(item => item.monthLabel)
  const createdData = trendData.value.trendData.map(item => item.createdCount)
  const completedData = trendData.value.trendData.map(item => item.completedCount)
  const overdueData = trendData.value.trendData.map(item => item.overdueCount)

  const option = {
    tooltip: {
      trigger: 'axis',
      axisPointer: {
        type: 'cross'
      }
    },
    legend: {
      data: ['创建任务', '完成任务', '逾期任务']
    },
    grid: {
      left: '3%',
      right: '3%',
      bottom: '10%',
      containLabel: true
    },
    xAxis: {
      type: 'category',
      boundaryGap: false,
      data: months,
      axisLabel: {
        interval: 0,
        rotate: 0
      }
    },
    yAxis: {
      type: 'value'
    },
    dataZoom: [
      {
        type: 'inside',
        start: 0,
        end: 100
      },
      {
        type: 'slider',
        height: 18,
        bottom: 0,
        start: 0,
        end: 100
      }
    ],
    series: [
      {
        name: '创建任务',
        type: 'line',
        data: createdData,
        smooth: true,
        symbol: 'circle',
        symbolSize: 6,
        itemStyle: {
          color: '#409EFF'
        }
      },
      {
        name: '完成任务',
        type: 'line',
        data: completedData,
        smooth: true,
        symbol: 'circle',
        symbolSize: 6,
        itemStyle: {
          color: '#67C23A'
        }
      },
      {
        name: '逾期任务',
        type: 'line',
        data: overdueData,
        smooth: true,
        symbol: 'circle',
        symbolSize: 6,
        itemStyle: {
          color: '#F56C6C'
        }
      }
    ]
  }

  lineChart.setOption(option, true)
}

// 饱和度颜色
const getSaturationColor = (rate) => {
  if (rate >= 3) return '#F56C6C'
  if (rate >= 2) return '#E6A23C'
  return '#67C23A'
}

// 完成率颜色
const getCompletionColor = (rate) => {
  if (rate >= 80) return '#67C23A'
  if (rate >= 60) return '#E6A23C'
  return '#F56C6C'
}

// 窗口大小变化时重绘图表
const handleResize = () => {
  pieChart?.resize()
  barChart?.resize()
  lineChart?.resize()
}

onMounted(() => {
  loadAllData()
  window.addEventListener('resize', handleResize)
})

onBeforeUnmount(() => {
  window.removeEventListener('resize', handleResize)
  pieChart?.dispose()
  barChart?.dispose()
  lineChart?.dispose()
  pieChart = null
  barChart = null
  lineChart = null
})
</script>

<style scoped>
.task-statistics {
  padding: 20px;
}

.chart-card :deep(.el-card__header) {
  padding: 12px 16px;
}

.chart-card :deep(.el-card__body) {
  padding: 12px 16px 16px;
}

.chart {
  width: 100%;
}

.chart-lg {
  height: 420px;
}

.chart-md {
  height: 360px;
}

.stat-card {
  text-align: center;
}

.stat-item {
  padding: 10px 0;
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

.stat-value.success {
  color: #67C23A;
}

.stat-value.primary {
  color: #409EFF;
}

.stat-value.danger {
  color: #F56C6C;
}
</style>
