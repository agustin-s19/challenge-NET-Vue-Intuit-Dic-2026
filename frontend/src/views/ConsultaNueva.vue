<template>
  <div class="card" style="max-width: 560px">
    <h2>Nueva consulta</h2>
    <form @submit.prevent="guardar">
      <div class="form-group">
        <label>Afiliado</label>
        <select v-model="form.afiliadoId">
          <option value="">Seleccioná un afiliado</option>
          <option v-for="a in afiliados" :key="a.id" :value="a.id">
            {{ a.nombreCompleto }} — DNI {{ a.dni }}
          </option>
        </select>
      </div>
      <div class="form-group">
        <label>Profesional</label>
        <select v-model="form.profesionalId">
          <option value="">Seleccioná un profesional</option>
          <option v-for="p in profesionales" :key="p.id" :value="p.id">
            {{ p.nombreCompleto }} — {{ p.especialidad }}
          </option>
        </select>
      </div>
      <div class="form-group">
        <label>Modalidad</label>
        <select v-model="form.modalidad">
          <option value="Presencial">Presencial</option>
          <option value="Virtual">Virtual</option>
        </select>
      </div>
      <div class="form-group">
        <label>Fecha y hora</label>
        <input type="datetime-local" v-model="form.fechaHora" :min="ahora" />
      </div>
      <div class="form-group">
        <label>Motivo</label>
        <input type="text" v-model="form.motivo" placeholder="Motivo de la consulta" />
      </div>
      <button type="submit" :disabled="guardando">
        {{ guardando ? 'Guardando...' : 'Confirmar consulta' }}
      </button>
    </form>
  </div>
</template>

<script>
import { consultasApi, afiliadosApi, profesionalesApi } from '../services/api'

function mensajeError(err) {
  return err.response?.data?.message
    || err.response?.data?.mensaje
    || 'Ocurrió un error inesperado. Intente nuevamente.'
}

export default {
  name: 'ConsultaNueva',
  data() {
    return {
      form: {
        afiliadoId: '',
        profesionalId: '',
        modalidad: 'Presencial',
        fechaHora: '',
        motivo: ''
      },
      afiliados: [],
      profesionales: [],
      guardando: false
    }
  },
  computed: {
    ahora() {
      const d = new Date()
      d.setMinutes(d.getMinutes() - d.getTimezoneOffset())
      return d.toISOString().slice(0, 16)
    }
  },
  async mounted() {
    try {
      const [aRes, pRes] = await Promise.all([afiliadosApi.getAll(), profesionalesApi.getAll()])
      this.afiliados = aRes.data
      this.profesionales = pRes.data
    } catch (err) {
      alert(mensajeError(err))
    }
  },
  methods: {
    validar() {
      if (!this.form.afiliadoId) return 'Seleccioná un afiliado.'
      if (!this.form.profesionalId) return 'Seleccioná un profesional.'
      if (!this.form.fechaHora) return 'Indicá la fecha y hora de la consulta.'
      if (new Date(this.form.fechaHora) <= new Date()) return 'La fecha y hora debe ser posterior al momento actual.'
      if (!this.form.motivo.trim()) return 'Indicá el motivo de la consulta.'
      return null
    },
    async guardar() {
      const error = this.validar()
      if (error) {
        alert(error)
        return
      }

      this.guardando = true
      try {
        await consultasApi.create({
          afiliadoId: Number(this.form.afiliadoId),
          profesionalId: Number(this.form.profesionalId),
          modalidad: this.form.modalidad,
          fechaHora: this.form.fechaHora,
          motivo: this.form.motivo.trim()
        })
        alert('Consulta agendada correctamente.')
        this.$router.push('/consultas')
      } catch (err) {
        alert(mensajeError(err))
      } finally {
        this.guardando = false
      }
    }
  }
}
</script>