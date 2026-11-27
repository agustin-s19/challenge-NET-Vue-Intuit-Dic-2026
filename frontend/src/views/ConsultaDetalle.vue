<template>
  <div>
    <button class="btn-primary" style="font-size:14px" @click="$router.back()">
      ← Volver
    </button>

    <div v-if="consulta" class="card" style="margin-top: 20px; max-width: 560px">
      <h2>Consulta #{{ consulta.id }}</h2>
      <div class="detail-row">
        <span class="label">Afiliado</span>
        <span>
          {{ consulta.afiliado?.nombreCompleto }}
          <span v-if="consulta.afiliado && !consulta.afiliado.isActive" class="badge-baja">Dado de baja</span>
        </span>
      </div>
      <div class="detail-row"><span class="label">DNI</span><span>{{ consulta.afiliado?.dni }}</span></div>
      <div class="detail-row"><span class="label">Profesional</span><span>{{ consulta.profesional?.nombreCompleto }}</span></div>
      <div class="detail-row"><span class="label">Especialidad</span><span>{{ consulta.profesional?.especialidad }}</span></div>
      <div class="detail-row"><span class="label">Fecha y hora</span><span>{{ formatFecha(consulta.fechaHora) }}</span></div>
      <div class="detail-row"><span class="label">Modalidad</span><span>{{ consulta.modalidad }}</span></div>
      <div class="detail-row"><span class="label">Estado</span><span>{{ consulta.estado }}</span></div>
      <div class="detail-row"><span class="label">Motivo</span><span>{{ consulta.motivo }}</span></div>

      <template v-if="estados.length">
        <div style="margin-top: 24px">
          <div class="form-group">
            <label>Cambiar estado</label>
            <select v-model="nuevoEstado">
              <option v-for="e in estados" :key="e" :value="e">{{ e }}</option>
            </select>
          </div>
          <button :disabled="procesando" @click="cambiarEstado" style="margin-bottom: 16px">Actualizar estado</button>
        </div>

        <div style="display: flex; gap: 10px">
          <button v-if="puede('Atendido')" :disabled="procesando" @click="registrarAtencion">Registrar atención</button>
          <button v-if="puede('Cancelado')" class="btn-danger" :disabled="procesando" @click="cancelar">Cancelar consulta</button>
          <button v-if="puede('Inasistencia')" :disabled="procesando" @click="marcarInasistencia">Marcar inasistencia</button>
        </div>
      </template>

      <p v-else class="estado-final">
        La consulta está en estado {{ consulta.estado }} y no admite más cambios.
      </p>
    </div>
    <p v-else>Cargando...</p>
  </div>
</template>

<script>
import { consultasApi } from '../services/api'

const TRANSICIONES = {
  Pendiente:    ['Confirmado', 'Cancelado', 'Atendido', 'Inasistencia'],
  Confirmado:   ['Cancelado', 'Atendido', 'Inasistencia'],
  Cancelado:    [],
  Atendido:     [],
  Inasistencia: []
}

function mensajeError(err) {
  return err.response?.data?.message
    || err.response?.data?.mensaje
    || 'Ocurrió un error inesperado. Intente nuevamente.'
}

export default {
  name: 'ConsultaDetalle',
  data() {
    return {
      consulta: null,
      nuevoEstado: '',
      procesando: false
    }
  },
  computed: {
    estados() {
      return TRANSICIONES[this.consulta?.estado] || []
    }
  },
  async mounted() {
    try {
      await this.recargar()
    } catch (err) {
      alert(err.response?.status === 404 ? 'La consulta no existe.' : mensajeError(err))
    }
  },
  methods: {
    puede(estado) {
      return this.estados.includes(estado)
    },

    formatFecha(fecha) {
      return new Date(fecha).toLocaleString('es-AR', {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
        hour12: false
      })
    },

    async recargar() {
      const res = await consultasApi.getById(this.$route.params.id)
      this.consulta = res.data
      this.nuevoEstado = this.estados[0] || ''
    },

    async ejecutar(accion, mensajeOk, pregunta) {
      if (pregunta && !confirm(pregunta)) return
      if (this.procesando) return

      this.procesando = true
      try {
        await accion()
        alert(mensajeOk)
        await this.recargar()
      } catch (err) {
        alert(mensajeError(err))
      } finally {
        this.procesando = false
      }
    },

    cambiarEstado() {
      if (!this.nuevoEstado) return
      return this.ejecutar(
        () => consultasApi.actualizarEstado(this.consulta.id, { estado: this.nuevoEstado }),
        `Estado actualizado a ${this.nuevoEstado}.`,
        `¿Cambiar el estado a ${this.nuevoEstado}?`
      )
    },

    registrarAtencion() {
      return this.ejecutar(
        () => consultasApi.registrarAtencion(this.consulta.id),
        'Atención registrada.',
        '¿Registrar la atención de esta consulta?'
      )
    },

    cancelar() {
      return this.ejecutar(
        () => consultasApi.cancelar(this.consulta.id),
        'Consulta cancelada.',
        '¿Cancelar esta consulta?'
      )
    },

    marcarInasistencia() {
      return this.ejecutar(
        () => consultasApi.marcarInasistencia(this.consulta.id),
        'Inasistencia registrada.',
        '¿Registrar inasistencia para esta consulta?'
      )
    }
  }
}
</script>

<style scoped>
.detail-row {
  display: flex;
  padding: 10px 0;
  border-bottom: 1px solid #f0f0f0;
  font-size: 14px;
}
.label {
  width: 130px;
  font-weight: 600;
  color: #666;
  flex-shrink: 0;
}
.badge-baja {
  margin-left: 6px;
  padding: 1px 8px;
  border-radius: 10px;
  font-size: 11px;
  font-weight: 600;
  background: #eceff1;
  color: #546e7a;
}
.estado-final {
  margin-top: 24px;
  font-size: 14px;
  color: #666;
}
button:disabled {
  opacity: 0.5;
  cursor: default;
}
</style>