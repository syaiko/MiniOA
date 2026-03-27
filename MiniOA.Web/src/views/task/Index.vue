<template>
	<div class="container">
		<h1>任务看板</h1>
		<div style="margin-bottom: 20px;">
			<el-button type="success" @click="showAddDialog = true">添加新任务</el-button>
		    <el-button type="primary" @click="fetchTasks">刷新任务</el-button>
		</div>
		
		<el-table :data="taskList" border stripe style="width:100%;margin-top:20px">
			<el-table-column prop="id" label="ID" width="80" sortable />
			<el-table-column prop="creatorName" label="创建人" width="100">
				<template #default="scope">
					{{ scope.row.creatorName || '未知' }}
				</template>
			</el-table-column>
			<el-table-column prop="title" label="任务标题" />
			<el-table-column prop="description" label="描述" />
			<el-table-column prop="priority" label="优先级" width="100" sortable :sort-method="sortByPriority">
				<template #default="scope">
					<el-tag :type="getPriorityDisplay(scope.row.priority).type" size="small">
						{{ getPriorityDisplay(scope.row.priority).label }}
					</el-tag>
				</template>
			</el-table-column>
			<el-table-column prop="dueDate" label="截止日期" width="180" sortable :sort-method="sortByDueDate">
				<template #default="scope">
					<span v-if="scope.row.dueDate || scope.row.DueDate">
						{{ formatDateTime(scope.row.dueDate || scope.row.DueDate) }}
					</span>
					<span v-else style="color: #999;">无截止日期</span>
				</template>
			</el-table-column>
			
			<el-table-column prop="status" label="状态" width="120" sortable :sort-method="sortByStatus">
				<template #default="scope">
					<el-tag :type="getStatusDisplay(scope.row.status).type">
						{{ getStatusDisplay(scope.row.status).label }}
					</el-tag>
				</template>
			</el-table-column>
			
			<el-table-column prop="createdAt" label="创建时间" width="180" sortable :sort-method="sortByCreatedAt">
				<template #default="scope">
					{{ formatDateTime(scope.row.createdAt) }}
				</template>
			</el-table-column>
			
			<el-table-column label="操作" width="400">
				<template #default="scope">
					<!-- 详细按钮 -->
					<el-button size="small" type="primary" @click="handleViewDetail(scope.row.id)">详细</el-button>
					
					<!-- 编辑按钮：待处理状态 -->
					<el-button
					  v-if="getStatusValue(scope.row.status) === 0"
					  size="small"
					  @click="handleEdit(scope.row)">
					    编辑
					</el-button>
					
					<!-- 删除按钮：待处理状态 -->
					<el-button
					  v-if="getStatusValue(scope.row.status) === 0"
					  size="small"
					  type="danger"
					  @click="handleDelete(scope.row.id)">
					    删除
					</el-button>
					
					<!-- 开始处理按钮：待处理状态 -->
					<el-button
					  v-if="getStatusValue(scope.row.status) === 0"
					  size="small"
					  type="primary"
					  @click="handleStartProcessing(scope.row.id)">
					    开始处理
					</el-button>
					
					<!-- 提交审核按钮：进行中或被驳回状态 -->
					<el-button
					  v-if="getStatusValue(scope.row.status) === 1 || getStatusValue(scope.row.status) === 4"
					  size="small"
					  @click="handleUpdateStatus(scope.row.id)">
					    提交审核
					</el-button>
					
					<!-- 审批按钮：审核中状态且是管理员 -->
					<template v-if="getStatusValue(scope.row.status) === 2 &&( userStore.isAdmin)"> <!-- 部门经理级以上可审批 -->
						<el-button size="small" type="success" @click="handleApprove(scope.row.id, true)">通过</el-button>
						<el-button size="small" type="danger" @click="handleApprove(scope.row.id, false)">驳回</el-button>
					</template>
					
					<!-- 查看审计日志按钮 -->
					<template v-if="userStore.isTeamLeader">  <!-- 组长级以上可看 -->
						<el-button size="small" type="info" @click="handleViewAuditLogs(scope.row.id)">审计日志</el-button>
					</template>
				</template>
			</el-table-column>
		</el-table>
		
		<el-dialog v-model="showAddDialog" title="新建任务" width="600px">
			<el-form :model="form" label-width="100px">
				<el-form-item label="标题">
					<el-input v-model="form.title" placeholder="请输入任务标题" />
				</el-form-item>
				
				<el-form-item label="描述">
					<el-input v-model="form.description" type="textarea" :rows="3" placeholder="请输入任务描述" />
				</el-form-item>
				
				<el-form-item label="优先级">
					<el-select v-model="form.priority" placeholder="请选择优先级" style="width: 100%">
						<el-option label="普通" value="Normal" />
						<el-option label="重要" value="Important" />
						<el-option label="紧急" value="Urgent" />
					</el-select>
				</el-form-item>
				
				<el-form-item label="指派部门">
					<el-tree-select
						v-model="form.departmentId"
						:data="departmentTreeData"
						:props="{ value: 'id', label: 'name', children: 'children' }"
						placeholder="请选择指派部门"
						clearable
						check-strictly
						:max-level="1"
						style="width: 100%"
					/>
				</el-form-item>
				
				<el-form-item label="指派用户">
					<el-select 
						v-model="form.assignedUserId" 
						:placeholder="form.departmentId ? '请选择指派用户' : '请先选择指派部门，指派人员只能选择指派部门里的成员'" 
						style="width: 100%" 
						clearable
						:disabled="!form.departmentId || userList.length === 0"
					>
						<el-option
							v-for="user in userList"
							:key="user.id"
							:label="user.fullName || user.username"
							:value="user.id"
						/>
					</el-select>
				</el-form-item>
				
				<el-form-item label="截止日期">
					<el-date-picker
						v-model="form.dueDate"
						type="datetime"
						placeholder="选择截止日期"
						format="YYYY-MM-DD HH:mm:ss"
						value-format="YYYY-MM-DD HH:mm:ss"
						style="width: 100%"
					/>
				</el-form-item>
			</el-form>
			
			<template #footer>
				<el-button @click="showAddDialog = false">取消</el-button>
			    <el-button type="primary" @click="handleCreate" :loading="submitting">提交</el-button>
			</template>
		</el-dialog>
		
		<!-- 审计日志对话框 -->
		<el-dialog v-model="showAuditLogsDialog" title="任务审计日志" width="800px">
			<el-table :data="auditLogs" border stripe max-height="400">
				<el-table-column prop="username" label="操作用户" width="120">
					<template #default="scope">
						{{ scope.row.fullName || scope.row.username }}
					</template>
				</el-table-column>
				<el-table-column prop="operationType" label="操作类型" width="120">
					<template #default="scope">
						{{ getOperationTypeDisplay(scope.row.operationType) }}
					</template>
				</el-table-column>
				<el-table-column label="状态变更" width="180">
					<template #default="scope">
						<el-tag :type="getStatusDisplay(scope.row.fromStatusDisplay).type" size="small">
							{{ getStatusDisplay(scope.row.fromStatusDisplay).label }}
						</el-tag>
						<span style="margin: 0 5px;">→</span>
						<el-tag :type="getStatusDisplay(scope.row.toStatusDisplay).type" size="small">
							{{ getStatusDisplay(scope.row.toStatusDisplay).label }}
						</el-tag>
					</template>
				</el-table-column>
				<el-table-column prop="remarks" label="备注" min-width="150" />
				<el-table-column prop="createdAt" label="操作时间" width="180">
					<template #default="scope">
						{{ formatDateTime(scope.row.createdAt) }}
					</template>
				</el-table-column>
			</el-table>
			
			<template #footer>
				<el-button @click="showAuditLogsDialog = false">关闭</el-button>
			</template>
		</el-dialog>
		
		<!-- 任务详情对话框 -->
		<el-dialog v-model="showDetailDialog" title="任务详情" width="600px">
			<el-descriptions v-if="detailTask" :column="1" border>
				<el-descriptions-item label="任务ID">{{ detailTask.id }}</el-descriptions-item>
				<el-descriptions-item label="任务标题">{{ detailTask.title }}</el-descriptions-item>
				<el-descriptions-item label="描述">{{ detailTask.description || '-' }}</el-descriptions-item>
				<el-descriptions-item label="创建人">{{ detailTask.creatorName || '未知' }}</el-descriptions-item>
				<el-descriptions-item label="指派部门">{{ detailTask.departmentName || '-' }}</el-descriptions-item>
				<el-descriptions-item label="指派用户">{{ detailTask.assignedUserName || '-' }}</el-descriptions-item>
				<el-descriptions-item label="优先级">
					<el-tag :type="getPriorityDisplay(detailTask.priority).type" size="small">
						{{ getPriorityDisplay(detailTask.priority).label }}
					</el-tag>
				</el-descriptions-item>
				<el-descriptions-item label="状态">
					<el-tag :type="getStatusDisplay(detailTask.status).type" size="small">
						{{ getStatusDisplay(detailTask.status).label }}
					</el-tag>
				</el-descriptions-item>
				<el-descriptions-item label="截止日期">
					{{ detailTask.dueDate ? formatDateTime(detailTask.dueDate) : '无截止日期' }}
				</el-descriptions-item>
				<el-descriptions-item label="创建时间">{{ formatDateTime(detailTask.createdAt) }}</el-descriptions-item>
			</el-descriptions>
			
			<template #footer>
				<el-button @click="showDetailDialog = false">关闭</el-button>
			</template>
		</el-dialog>
	</div>
</template>

<script setup>
	import {ref, reactive, onMounted, watch} from 'vue'
	import { 
		getMyTasks,
		getTask,
		createTask,
		updateTask,
		deleteTask,
		startProcessing,
		submitReview,
		approveTask,
		getTaskAuditLogs
	} from '../../api/task.js'
	import { getDepartmentTree } from '../../api/department.js'
	import { getUsers } from '../../api/user.js'
	import { ElMessage, ElMessageBox } from 'element-plus'
	import { useUserStore } from '../../store/user.js'
	
	const userStore = useUserStore()
	
	//数据状态
	const taskList = ref([])
	const showAddDialog = ref(false)
	const submitting = ref(false)
	const showAuditLogsDialog = ref(false)
	const auditLogs = ref([])
	const departmentTreeData = ref([])
	const userList = ref([])
	const showDetailDialog = ref(false)
	const detailTask = ref(null)
	
	//状态字典映射
	const statusMap ={
		'Todo': { label: '待处理', type: 'info', value: 0 },
		'Processing': { label: '进行中', type: 'primary', value: 1 },
	    'Review': { label: '审核中', type: 'warning', value: 2 },
		'Completed': { label: '已完成', type: 'success', value: 3 },
		'Rejected': { label: '已驳回', type: 'danger', value: 4 },
		'Overdue': { label: '已逾期', type: 'danger', value: 5 }
	}
	
	//优先级字典映射
	const priorityMap = {
		'Normal': { label: '普通', type: 'info' },
		'Important': { label: '重要', type: 'warning' },
		'Urgent': { label: '紧急', type: 'danger' }
	}
	
	//操作类型字典映射
	const operationTypeMap = {
		'Create': '创建任务',
		'StatusChange': '状态变更',
		'Assign': '任务指派',
		'Update': '更新任务',
		'Delete': '删除任务'
	}
	
	//获取状态显示
	const getStatusDisplay = (status) => {
		return statusMap[status] || { label: '未知状态', type: 'info', value: -1 }
	}
	
	//获取优先级显示
	const getPriorityDisplay = (priority) => {
		if (typeof priority === 'number') {
			// 数字转换为对应的字符串键
			const priorityKeys = ['Normal', 'Important', 'Urgent']
			const priorityKey = priorityKeys[priority] || 'Normal'
			return priorityMap[priorityKey] || { label: '未知', type: 'info' }
		} else {
			// 字符串直接使用
			return priorityMap[priority] || { label: '未知', type: 'info' }
		}
	}
	
	//获取操作类型显示
	const getOperationTypeDisplay = (operationType) => {
		return operationTypeMap[operationType] || operationType
	}
	
	//获取优先级数值
	const getPriorityValue = (priority) => {
		const priorityValueMap = {
			'Normal': 0,
			'Important': 1,
			'Urgent': 2
		}
		return priorityValueMap[priority] || 0
	}
	
	//获取状态数值
	const getStatusValue = (status) => {
		return getStatusDisplay(status).value
	}
	
	//按优先级排序
	const sortByPriority = (a, b) => {
	    const priorityOrder = {
	      'Normal': 0,
	      'Important': 1,
	      'Urgent': 2,
	      // 如果有数字类型也兼容（虽然目前后端返回字符串）
	      0: 0,
	      1: 1,
	      2: 2
	    }
	
	    const valueA = priorityOrder[a.priority] ?? 0
	    const valueB = priorityOrder[b.priority] ?? 0
	
	    return valueA - valueB
	}
	
	//按状态排序
	const sortByStatus = (a, b) => {
		return getStatusValue(a.status) - getStatusValue(b.status)
	}
	
	//按日期排序（适用于截止日期）
	const sortByDueDate = (rowA, rowB) => {
	    // 空值（null/undefined/空字符串）放最后
	    if (!rowA.dueDate && !rowB.dueDate) return 0
	    if (!rowA.dueDate) return 1
	    if (!rowB.dueDate) return -1
	
	    // 统一加上 'Z'，明确是 UTC
	    const dateA = new Date(rowA.dueDate + 'Z')
	    const dateB = new Date(rowB.dueDate + 'Z')
	
	    // 无效日期放最后
	    if (isNaN(dateA.getTime()) && isNaN(dateB.getTime())) return 0
	    if (isNaN(dateA.getTime())) return 1
	    if (isNaN(dateB.getTime())) return -1
	
	    return dateA - dateB 
	}
	//按日期排序（适用于创建日期）
	const sortByCreatedAt = (rowA, rowB) => {
	    // createdAt 理论上不可能为空，但以防万一
	    if (!rowA.createdAt && !rowB.createdAt) return 0
	    if (!rowA.createdAt) return 1
	    if (!rowB.createdAt) return -1
	
	    const dateA = new Date(rowA.createdAt + 'Z')
	    const dateB = new Date(rowB.createdAt + 'Z')
	
	    if (isNaN(dateA.getTime()) && isNaN(dateB.getTime())) return 0
	    if (isNaN(dateA.getTime())) return 1
	    if (isNaN(dateB.getTime())) return -1
	
	    return dateA - dateB
	}
	
	//格式化日期时间
	const formatDateTime = (dateTime) => {
		if (!dateTime) return ''
		  
	    let isoString = dateTime
	    // 如果没有 T，尝试修复
	    if (!dateTime.includes('T') && dateTime.includes(' ')) {
		  isoString = dateTime.replace(' ', 'T') + 'Z'   // 明确告诉它是 UTC
	    }
	    // 如果已经有 T 但没带 Z，也补上
	    else if (!dateTime.endsWith('Z') && !dateTime.includes('+') && !dateTime.includes('-', 10)) {
		  isoString = dateTime + 'Z'
	    }
	
	    const date = new Date(isoString)
	    if (isNaN(date.getTime())) return '无效日期'
	
	    return date.toLocaleString('zh-CN', {
		  timeZone: 'Asia/Shanghai',
		  year: 'numeric',
		  month: '2-digit',
		  day: '2-digit',
		  hour: '2-digit',
		  minute: '2-digit',
		  second: '2-digit',
		  hour12: false
	    }).replace(/\//g, '-')
	}
	
	//表单对象
	const form = reactive({
		title:'',
		description:'',
		priority: 'Normal',
		dueDate: '',
		departmentId: null,
		assignedUserId: null
	})
	
	//获取列表
	const fetchTasks = async () => {
		try {
			const res = await getMyTasks()
			if (res.success) {
				taskList.value = res.data || []
			}
		} catch (error) {
			console.error('获取任务列表失败:', error)
		}
	}
	
	//获取部门树数据
	const fetchDepartments = async () => {
		try {
			const res = await getDepartmentTree()
			// 处理 API 响应结构
			if (res && res.success) {
				let departments = res.data || []
				
				// 根据用户角色过滤部门
				const userRole = userStore.userInfo?.role
				const userDeptId = userStore.userInfo?.departmentId
				
				if (userRole > 1) { // 非管理员及以上
					if (userRole === 4) { // 普通员工
						// 只能看到自己部门
						const filterDept = (depts) => {
							if (depts.id === userDeptId) return true
							if (depts.children) {
								return depts.children.some(child => filterDept(child))
							}
							return false
						}
						departments = departments.filter(filterDept)
					}
					// 组长及以上（2、3）可以看到所有部门（跨部门指派）
				}
				
				departmentTreeData.value = departments
			} else {
				departmentTreeData.value = []
			}
		} catch (error) {
			console.error('获取部门列表失败:', error)
			departmentTreeData.value = []
		}
	}
	
	//获取用户列表
	const fetchUsers = async () => {
		try {
			// 如果没有选择部门，清空用户列表
			if (!form.departmentId) {
				userList.value = []
				return
			}
			
			const res = await getUsers(form.departmentId)
			// 处理 API 响应结构
			if (res && res.success) {
				let users = res.data || []
				
				// 根据用户角色和选择的部门过滤用户
				const userRole = userStore.userInfo?.role
				const userDeptId = userStore.userInfo?.departmentId
				
				// 只有非管理员才需要限制
				if (userRole > 1) { // 非管理员及以上（2,3,4）
					if (userRole === 4) { // 普通员工
						// 只能看到自己部门的用户
						if (form.departmentId !== userDeptId) {
							users = []
						}
					} else { // 组长及以上（2,3）
						// 跨部门时无法选择成员
						if (form.departmentId !== userDeptId) {
							users = []
						}
					}
				}
				// 管理员（role=0,1）无限制，可以看到所有用户
				
				userList.value = users
			} else {
				userList.value = []
			}
		} catch (error) {
			console.error('获取用户列表失败:', error)
			userList.value = []
		}
	}
	
	//提交创建或编辑
	const handleCreate = async () => {
		if(!form.title?.trim()) return ElMessage.warning('标题不能为空')
		if(form.title.trim().length < 2) return ElMessage.warning('标题长度不能少于2个字符')
		
		// 验证指派逻辑
		if (userStore.userInfo?.role > 1) { // 非管理员及以上
			if (!form.departmentId) {
				return ElMessage.warning('请选择指派部门')
			}
			if (userStore.userInfo?.role !== 2 && !form.assignedUserId) { // 非部门经理
				return ElMessage.warning('请选择指派人员')
			}
		}
		
		submitting.value = true
		try{
			const submitData = {
				Title: form.title.trim(),
				Description: form.description?.trim() || '',
				Priority: getPriorityValue(form.priority),
				DueDate: form.dueDate ? new Date(form.dueDate) : null,
				DepartmentId: form.departmentId,
				AssignedUserId: form.assignedUserId
			}
			
			if (editingTask.value) {
				// 编辑模式
				await updateTask(editingTask.value.id, submitData)
				ElMessage.success('任务更新成功')
			} else {
				// 创建模式
				await createTask(submitData)
				ElMessage.success('任务创建成功')
			}
			
			showAddDialog.value = false  //关闭弹窗
			// 重置表单
			form.title = ''
			form.description = ''
			form.priority = 'Normal'
			form.dueDate = ''
			form.departmentId = null
			form.assignedUserId = null
			editingTask.value = null
			await fetchTasks()
		} catch (error) {
			// 错误信息已在request.js的拦截器中处理，这里只需要记录日志
			console.error('操作失败:', error)
		} finally {
			submitting.value = false
		}
	}
	
	// 监听部门变化
	watch(() => form.departmentId, (newDeptId) => {
		if (newDeptId) {
			// 部门变化时，重新获取用户列表
			fetchUsers()
		} else {
			// 清空部门时，清空用户选择
			form.assignedUserId = null
			userList.value = []
		}
	})
	
	//查看任务详情
	const handleViewDetail = async (id) => {
		try {
			const res = await getTask(id)
			// 处理 API 响应结构
			if (res && res.success) {
				detailTask.value = res.data
				showDetailDialog.value = true
			} else {
				ElMessage.error(res?.message || '获取任务详情失败')
			}
		} catch (error) {
			console.error('获取任务详情失败:', error)
			ElMessage.error('获取任务详情失败')
		}
	}
	
	//编辑任务
	const editingTask = ref(null)
	const handleEdit = async (task) => {
		editingTask.value = task
		form.title = task.title
		form.description = task.description || ''
		form.priority = getPriorityDisplay(task.priority).label === '重要' ? 'Important' : 
						getPriorityDisplay(task.priority).label === '紧急' ? 'Urgent' : 'Normal'
		form.dueDate = task.dueDate || ''
		form.departmentId = task.departmentId || null
		form.assignedUserId = task.assignedUserId || null
		
		if (form.departmentId) {
			await fetchUsers()
		}
		showAddDialog.value = true
	}
	
	//删除任务
	const handleDelete = async (id) => {
		try {
			await ElMessageBox.confirm('确定要删除该任务吗？', '删除确认', {
				confirmButtonText: '确定',
				cancelButtonText: '取消',
				type: 'warning'
			})
			
			await deleteTask(id)
			ElMessage.success('删除成功')
			await fetchTasks()
		} catch (error) {
			if (error !== 'cancel') {
				console.error('删除任务失败:', error)
			}
		}
	}
	
	//开始处理任务--员工
	const handleStartProcessing = async (id) => {
		try {
			await startProcessing(id)
			ElMessage.success('已开始处理任务')
			await fetchTasks()
		} catch (error) {
			// 错误信息已在request.js的拦截器中处理，这里只需要记录日志
			console.error('开始处理任务失败:', error)
		}
	}
	
	//处理任务进度--员工
	const handleUpdateStatus = async (id) => {
		try {
			await submitReview(id)
			ElMessage.success('已提交审核')
			await fetchTasks()
		} catch (error) {
			// 错误信息已在request.js的拦截器中处理，这里只需要记录日志
			console.error('提交审核失败:', error)
		}
	}
	
	const handleApprove = async (id, isPass) => {
		try {
			await approveTask(id,isPass)
			ElMessage.success(isPass ? '审批通过' : '已驳回')
			await fetchTasks() // 刷新列表
		} catch (error) {
			// 错误信息已在request.js的拦截器中处理，这里只需要记录日志
			console.error('审批失败:', error)
		}
	}
	
	// 查看审计日志
	const handleViewAuditLogs = async (id) => {
		try {
			const res = await getTaskAuditLogs(id)
			// 处理 API 响应结构
			if (res && res.success) {
				auditLogs.value = res.data || []
			} else {
				auditLogs.value = []
			}
			showAuditLogsDialog.value = true
		} catch (error) {
			console.error('获取审计日志失败:', error)
			auditLogs.value = []
			showAuditLogsDialog.value = true
		}
	}
	
	onMounted(() => {
		fetchTasks()
		fetchDepartments()
		fetchUsers()
	})
</script>

<style scoped>
	.container { padding: 20px;}
</style>