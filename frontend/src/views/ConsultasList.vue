<template>
  <div>
    <h2>Consultas</h2>

    <div class="filtros">
      <input
        v-model="filtros.texto"
        class="busqueda"
        type="search"
        placeholder="Buscar por afiliado, DNI, profesional o especialidad"
      />

      <select v-model="filtros.estado">
        <option value="">Todos los estados</option>
        <option v-for="e in estados" :key="e" :value="e">{{ e }}</option>
      </select>

      <span v-if="filtros.afiliadoId" class="chip">
        Afiliado: {{ nombreAfiliadoFiltrado }}
        <button @click="filtros.afiliadoId = ''">✕</button>
      </span>

      <button v-if="hayFiltros" @click="limpiarFiltros">Limpiar filtros</button>

      <span class="contador">
        {{ consultasFiltradas.length }} resultado{{ consultasFiltradas.length === 1 ? '' : 's' }}
        <template v-if="consultasFiltradas.length !== consultas.length"> de {{ consultas.length }}</template>
      </span>
    </div>

    <table v-if="consultasFiltradas.length">
      <thead>
        <tr>
          <th class="ordenable" @click="ordenarPor('id')">#{{ flecha('id') }}</th>
          <th class="ordenable" @click="ordenarPor('afiliado')">Afiliado{{ flecha('afiliado') }}</th>
          <th class="ordenable" @click="ordenarPor('profesional')">Profesional{{ flecha('profesional') }}</th>
          <th class="ordenable" @click="ordenarPor('especialidad')">Especialidad{{ flecha('especialidad') }}</th>
          <th class="ordenable" @click="ordenarPor('fechaHora')">Fecha y hora{{ flecha('fechaHora') }}</th>
          <th class="ordenable" @click="ordenarPor('modalidad')">Modalidad{{ flecha('modalidad') }}</th>
          <th class="ordenable" @click="ordenarPor('estado')">Estado{{ flecha('estado') }}</th>
          <th>Motivo</th>
          <th>Acciones</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="consulta in consultasPaginadas" :key="consulta.id">
          <td>{{ consulta.id }}</td>
          <td>{{ consulta.afiliado?.nombreCompleto }}</td>
          <td>{{ consulta.profesional?.nombreCompleto }}</td>
          <td>{{ consulta.profesional?.especialidad }}</td>
          <td>{{ formatFecha(consulta.fechaHora) }}</td>
          <td>{{ consulta.modalidad }}</td>
          <td>
            <span :class="['badge', `badge-${consulta.estado?.toLowerCase()}`]">{{ consulta.estado }}</span>
          </td>
          <td>{{ consulta.motivo }}</td>
          <td>
            <div class="acciones">
              <button class="btn-primary" @click="$router.push(`/consultas/${consulta.id}`)">Ver</button>
              <button class="btn-danger" @click="cancelar(consulta.id)">Cancelar</button>
            </div>
          </td>
        </tr>
      </tbody>
    </table>

    <div v-if="consultasFiltradas.length" class="paginacion">
      <button :disabled="pagina === 1" @click="pagina = 1">«</button>
      <button :disabled="pagina === 1" @click="pagina--">‹ Anterior</button>
      <span>Página {{ pagina }} de {{ totalPaginas }}</span>
      <button :disabled="pagina === totalPaginas" @click="pagina++">Siguiente ›</button>
      <button :disabled="pagina === totalPaginas" @click="pagina = totalPaginas">»</button>

      <select v-model.number="porPagina" @change="pagina = 1">
        <option :value="10">10 por página</option>
        <option :value="25">25 por página</option>
        <option :value="50">50 por página</option>
      </select>
    </div>

    <p v-else-if="consultas.length">No hay consultas que coincidan con los filtros.</p>
    <p v-else>No hay consultas registradas.</p>
  </div>
</template>

<script>
import { consultasApi } from '../services/api'

function mensajeError(err) {
  return err.response?.data?.message
    || err.response?.data?.mensaje
    || 'Ocurrió un error inesperado. Intente nuevamente.'
}

function normalizar(texto) {
  return (texto || '')
    .toString()
    .toLowerCase()
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
}

export default {
  name: 'ConsultasList',
  data() {
    return {
      consultas: [],
      estados: ['Pendiente', 'Confirmado', 'Cancelado', 'Atendido', 'Inasistencia'],
      filtros: {
        texto: this.$route.query.q || '',
        estado: this.$route.query.estado || '',
        afiliadoId: this.$route.query.afiliadoId || ''
      },
      orden: { campo: 'fechaHora', asc: false },
      pagina: 1,
      porPagina: 10
    }
  },
  computed: {
    hayFiltros() {
      return this.filtros.texto || this.filtros.estado || this.filtros.afiliadoId
    },
    nombreAfiliadoFiltrado() {
      const c = this.consultas.find(c => String(c.afiliadoId) === String(this.filtros.afiliadoId))
      return c?.afiliado?.nombreCompleto || `#${this.filtros.afiliadoId}`
    },
    consultasFiltradas() {
      const f = this.filtros
      const texto = normalizar(f.texto.trim())

      const filtradas = this.consultas.filter(c => {
        if (f.afiliadoId && String(c.afiliadoId) !== String(f.afiliadoId)) return false
        if (f.estado && c.estado !== f.estado) return false
        if (!texto) return true

        const campos = [
          c.afiliado?.nombreCompleto,
          c.afiliado?.dni,
          c.profesional?.nombreCompleto,
          c.profesional?.especialidad,
          c.motivo
        ]
        return campos.some(v => normalizar(v).includes(texto))
      })

      const valor = c => {
        switch (this.orden.campo) {
          case 'fechaHora':    return new Date(c.fechaHora).getTime()
          case 'afiliado':     return c.afiliado?.nombreCompleto || ''
          case 'profesional':  return c.profesional?.nombreCompleto || ''
          case 'especialidad': return c.profesional?.especialidad || ''
          default:             return c[this.orden.campo] ?? ''
        }
      }

      return filtradas.sort((a, b) => {
        const va = valor(a), vb = valor(b)
        const r = va < vb ? -1 : va > vb ? 1 : 0
        return this.orden.asc ? r : -r
      })
    },
    totalPaginas() {
      return Math.max(1, Math.ceil(this.consultasFiltradas.length / this.porPagina))
    },
    consultasPaginadas() {
      const inicio = (this.pagina - 1) * this.porPagina
      return this.consultasFiltradas.slice(inicio, inicio + this.porPagina)
    }
  },
  watch: {
    filtros: {
      deep: true,
      handler(f) {
        this.pagina = 1
        const query = {}
        if (f.texto) query.q = f.texto
        if (f.estado) query.estado = f.estado
        if (f.afiliadoId) query.afiliadoId = f.afiliadoId
        this.$router.replace({ query })
      }
    },
    orden: {
      deep: true,
      handler() {
        this.pagina = 1
      }
    },
    totalPaginas(total) {
      if (this.pagina > total) this.pagina = total
    }
  },
  async mounted() {
    await this.cargarConsultas()
  },
  methods: {
    async cargarConsultas() {
      try {
        const res = await consultasApi.getAll()
        this.consultas = res.data
      } catch (err) {
        alert(mensajeError(err))
      }
    },
    limpiarFiltros() {
      this.filtros = { texto: '', estado: '', afiliadoId: '' }
    },
    ordenarPor(campo) {
      if (this.orden.campo === campo) this.orden.asc = !this.orden.asc
      else this.orden = { campo, asc: true }
    },
    flecha(campo) {
      if (this.orden.campo !== campo) return ''
      return this.orden.asc ? ' ▲' : ' ▼'
    },
    formatFecha(fecha) {
      return new Date(fecha).toLocaleString('es-AR', {
        day: '2-digit', month: '2-digit', year: 'numeric',
        hour: '2-digit', minute: '2-digit', hour12: false
      })
    },
    async cancelar(id) {
      if (!confirm(`¿Desea cancelar la consulta ${id}?`)) return
      try {
        await consultasApi.cancelar(id)
      } catch (err) {
        alert(mensajeError(err))
        return
      }
      alert(`La consulta ${id} se canceló correctamente.`)
      await this.cargarConsultas()
    }
  }
}
</script>

<style scoped>
.filtros {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 10px;
  margin-bottom: 16px;
}
.busqueda {
  flex: 1;
  min-width: 280px;
  padding: 6px 10px;
}
.chip {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 4px 10px;
  border-radius: 14px;
  background: #e8f0fe;
  color: #1a73e8;
  font-size: 13px;
}
.chip button {
  margin: 0;
  padding: 0 4px;
  background: none;
  border: none;
  color: inherit;
  cursor: pointer;
}
.contador {
  margin-left: auto;
  font-size: 13px;
  color: #666;
}
.ordenable {
  cursor: pointer;
  user-select: none;
}
.ordenable:hover {
  text-decoration: underline;
}
.badge {
  padding: 3px 10px;
  border-radius: 12px;
  font-size: 12px;
  font-weight: 600;
}
.badge-pendiente    { background: #fff3cd; color: #856404; }
.badge-confirmado   { background: #d4edda; color: #155724; }
.badge-cancelado    { background: #f8d7da; color: #721c24; }
.badge-atendido     { background: #d1ecf1; color: #0c5460; }
.badge-inasistencia { background: #e2e3e5; color: #383d41; }
.acciones {
  display: flex;
  gap: 8px;
  white-space: nowrap;
}
.acciones button {
  margin: 0;
}
.paginacion {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  margin-top: 16px;
  font-size: 14px;
}
.paginacion button:disabled {
  opacity: 0.4;
  cursor: default;
}
.paginacion select {
  margin-left: 16px;
}
</style>