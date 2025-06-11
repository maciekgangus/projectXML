<template>
  <div id="app" class="container mx-auto p-6 font-sans">
    <h1 class="text-3xl font-bold mb-6">Organization Tree Manager</h1>

    <!-- Formularz tworzenia drzewa -->
    <div class="mb-8 bg-gray-100 p-6 rounded-lg shadow">
      <h3 class="text-xl font-semibold mb-4">Create New Tree</h3>
      <input
        v-model="newTreeName"
        placeholder="Tree Name"
        class="w-full p-3 mb-3 border rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
      />
      <textarea
        v-model="newTreeXml"
        placeholder="XML Data (e.g., <node>content</node>)"
        rows="4"
        class="w-full p-3 mb-3 border rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
      ></textarea>
      <button
        @click="createTree"
        class="bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700 transition"
      >
        Create Tree
      </button>
    </div>

    <!-- Lista drzew -->
    <div>
      <h3 class="text-xl font-semibold mb-4">Trees</h3>
      <ul class="space-y-4">
        <li v-for="tree in trees" :key="tree.id" class="bg-gray-50 p-4 rounded-lg shadow">
          <div
            class="flex justify-between items-center cursor-pointer"
            @click="toggleTree(tree.id)"
          >
            <span class="font-medium">{{ tree.treeName }} (ID: {{ tree.id }})</span>
            <span class="text-sm text-gray-500">{{
              expandedTrees.includes(tree.id) ? 'Collapse' : 'Expand'
            }}</span>
          </div>
          <div v-if="expandedTrees.includes(tree.id)" class="mt-4 bg-white p-4 rounded-lg">
            <pre class="bg-gray-100 p-3 rounded-lg">{{ xmlTreeData(tree.treeData) }}</pre>

            <div class="mt-4">
              <h4 class="font-semibold cursor-pointer" @click="toggleSection(tree.id, 'add')">
                Add Node {{ expandedSections[tree.id]?.add ? '▲' : '▼' }}
              </h4>
              <div v-if="expandedSections[tree.id]?.add" class="mt-2">
                <textarea
                  v-model="newNodeValue"
                  placeholder="Node XML (e.g., <Person><Name>Ewa Kowalska</Name></Person>)"
                  rows="4"
                  class="w-full p-3 mb-2 border rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                ></textarea>
                <input
                  v-model="parentPath"
                  placeholder="Parent Path (e.g., /Root/Person[@id='1']/Children)"
                  class="w-full p-3 mb-2 border rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
                <button
                  @click="addNode(tree.id)"
                  class="bg-yellow-500 text-white px-4 py-2 rounded-lg hover:bg-yellow-600 transition"
                >
                  Add Node
                </button>
              </div>
            </div>

            <div class="mt-4">
              <h4 class="font-semibold cursor-pointer" @click="toggleSection(tree.id, 'remove')">
                Remove Node {{ expandedSections[tree.id]?.remove ? '▲' : '▼' }}
              </h4>
              <div v-if="expandedSections[tree.id]?.remove" class="mt-2">
                <input
                  v-model="removePath"
                  placeholder="Node Path (e.g., /Root/Person[@id='1'])"
                  class="w-full p-3 mb-2 border rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
                <button
                  @click="removeNode(tree.id)"
                  class="bg-red-500 text-white px-4 py-2 rounded-lg hover:bg-red-600 transition"
                >
                  Remove Node
                </button>
              </div>
            </div>

            <div class="mt-4">
              <h4 class="font-semibold cursor-pointer" @click="toggleSection(tree.id, 'report')">
                Generate Report {{ expandedSections[tree.id]?.report ? '▲' : '▼' }}
              </h4>
              <div v-if="expandedSections[tree.id]?.report" class="mt-2">
                <input
                  v-model="reportPath"
                  placeholder="XPath (e.g., /Root/Person[@id='1'])"
                  class="w-full p-3 mb-2 border rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
                <button
                  @click="generateReport(tree.id)"
                  class="bg-indigo-600 text-white px-4 py-2 rounded-lg hover:bg-indigo-700 transition"
                >
                  Generate Report
                </button>
                <pre
                  v-if="reportXml"
                  class="mt-2 bg-gray-100 p-3 rounded-lg whitespace-pre-wrap font-mono text-sm"
                >{{ renderXmlTree(reportXml) }}</pre>
              </div>
            </div>

            <button
              @click="deleteTree(tree.id)"
              class="bg-red-600 text-white px-4 py-2 rounded-lg hover:bg-red-700 transition mt-4"
            >
              Delete Tree
            </button>
          </div>
        </li>
      </ul>
    </div>

    <!-- Komunikaty -->
    <p v-if="message" class="mt-4 p-3 bg-green-100 text-green-700 rounded-lg">{{ message }}</p>
    <p v-if="error" class="mt-4 p-3 bg-red-100 text-red-700 rounded-lg">{{ error }}</p>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import xmlFormat from 'xml-formatter'

const trees = ref([])
const selectedTree = ref(null)
const newTreeName = ref('')
const newTreeXml = ref('')
const newNodeValue = ref('')
const parentPath = ref('')
const removePath = ref('')
const message = ref('')
const error = ref('')
const expandedTrees = ref([])
const expandedSections = ref({})
const reportPath = ref('')
const reportXml = ref('')

const apiUrl = 'http://localhost:5000/api/OrganizationTree'

function renderXmlTree(xmlString) {
  try {
    const doc = new DOMParser().parseFromString(xmlString, 'application/xml')
    const root = doc.documentElement
    if (!root || root.nodeName === 'parsererror') return 'Invalid XML'
    return renderNode(root, 0).join('\n')
  } catch {
    return 'Error parsing XML'
  }
}

function renderNode(node, indentLevel = 0) {
  const lines = []
  const indent = '  '.repeat(indentLevel)

  const children = Array.from(node.children)
  const isLeaf = children.length === 0

  if (!isLeaf) {
    lines.push(`${indent}• ${node.nodeName}:`)
    children.forEach((child) => {
      lines.push(...renderNode(child, indentLevel + 1))
    })
  } else {
    lines.push(`${indent}- (${node.nodeName})${node.textContent?.trim()}`)
  }

  return lines
}

const generateReport = async (treeId) => {
  const encoded = encodeURIComponent(reportPath.value || '')
  try {
    const response = await fetch(`${apiUrl}/${treeId}/report?path=${encoded}`)
    if (response.ok) {
      reportXml.value = await response.text()
      message.value = 'Report generated'
    } else {
      error.value = await response.text()
    }
  } catch (err) {
    error.value = 'Error: ' + err.message
  }
}

const xmlTreeData = (treeData) => {
  return treeData ? xmlFormat(treeData, { collapseContent: true }) : ''
}

function isValidXml(xmlString) {
  try {
    const parser = new DOMParser()
    const doc = parser.parseFromString(xmlString, 'application/xml')
    return !doc.querySelector('parsererror')
  } catch {
    return false
  }
}

const fetchTrees = async () => {
  try {
    const response = await fetch(apiUrl)
    if (response.ok) {
      trees.value = await response.json()
      error.value = ''
    } else {
      error.value = 'Failed to fetch trees'
    }
  } catch (err) {
    error.value = 'Error: ' + err.message
  }
}

const toggleTree = (treeId) => {
  if (expandedTrees.value.includes(treeId)) {
    expandedTrees.value = expandedTrees.value.filter((id) => id !== treeId)
    delete expandedSections.value[treeId]
    if (selectedTree.value?.id === treeId) selectedTree.value = null
  } else {
    expandedTrees.value.push(treeId)
    expandedSections.value[treeId] = { add: false, remove: false, edit: false }
    selectTree(treeId)
  }
}

const toggleSection = (treeId, section) => {
  expandedSections.value[treeId] = {
    ...expandedSections.value[treeId],
    [section]: !expandedSections.value[treeId]?.[section],
  }
}

const selectTree = async (id) => {
  try {
    const response = await fetch(`${apiUrl}/${id}`)
    if (response.ok) {
      selectedTree.value = await response.json()
      error.value = ''
    } else {
      error.value = 'Failed to fetch tree'
    }
  } catch (err) {
    error.value = 'Error: ' + err.message
  }
}

const createTree = async () => {
  if (!newTreeName.value || !newTreeXml.value) {
    error.value = 'Tree name and XML data are required'
    return
  }
  const xml = `<Tree><TreeName>${newTreeName.value}</TreeName>${newTreeXml.value}</Tree>`
  try {
    const response = await fetch(apiUrl, {
      method: 'POST',
      headers: { 'Content-Type': 'application/xml' },
      body: xml,
    })
    if (response.ok) {
      message.value = 'Tree created successfully'
      newTreeName.value = ''
      newTreeXml.value = ''
      await fetchTrees()
    } else {
      error.value = await response.text()
    }
  } catch (err) {
    error.value = 'Error: ' + err.message
  }
}

const addNode = async (treeId) => {
  const raw = newNodeValue.value.trim()

  if (!raw) {
    error.value = 'Node XML is required'
    return
  }

  if (!isValidXml(raw)) {
    error.value = 'Invalid XML format – check if it starts with a valid <Tag>'
    return
  }

  const nodeXml = `<NewNode parent="${parentPath.value || ''}">${raw}</NewNode>`

  try {
    const response = await fetch(`${apiUrl}/${treeId}/addnode`, {
      method: 'PATCH',
      headers: { 'Content-Type': 'application/xml' },
      body: nodeXml,
    })

    if (response.ok) {
      message.value = 'Node added successfully'
      newNodeValue.value = ''
      parentPath.value = ''
      await selectTree(treeId)

      // 💡 wymuś aktualizację drzewa w liście
      const index = trees.value.findIndex((t) => t.id === treeId)
      if (index !== -1 && selectedTree.value) {
        trees.value[index] = { ...selectedTree.value }
      }
    } else {
      error.value = await response.text()
    }
  } catch (err) {
    error.value = 'Error: ' + err.message
  }
}

const removeNode = async (treeId) => {
  if (!removePath.value) {
    error.value = 'Node path is required'
    return
  }

  const nodeXml = `<RemoveNode path="${removePath.value}"/>`

  try {
    const response = await fetch(`${apiUrl}/${treeId}/removenode`, {
      method: 'DELETE',
      headers: { 'Content-Type': 'application/xml' },
      body: nodeXml,
    })

    if (response.ok) {
      message.value = 'Node removed successfully'
      newNodeValue.value = ''
      parentPath.value = ''
      await selectTree(treeId)

      // 💡 wymuś aktualizację drzewa w liście
      const index = trees.value.findIndex((t) => t.id === treeId)
      if (index !== -1 && selectedTree.value) {
        trees.value[index] = { ...selectedTree.value }
      }
    } else {
      error.value = await response.text()
    }
  } catch (err) {
    error.value = 'Error: ' + err.message
  }
}

const deleteTree = async (treeId) => {
  try {
    const response = await fetch(`${apiUrl}/${treeId}`, {
      method: 'DELETE',
    })
    if (response.ok) {
      message.value = 'Tree deleted successfully'
      expandedTrees.value = expandedTrees.value.filter((id) => id !== treeId)
      delete expandedSections.value[treeId]
      selectedTree.value = null
      await fetchTrees()
    } else {
      error.value = 'Failed to delete tree'
    }
  } catch (err) {
    error.value = 'Error: ' + err.message
  }
}

onMounted(fetchTrees)
</script>