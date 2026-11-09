<script setup lang="ts">
import { computed } from 'vue';

interface Props {
  modelValue: string | number;
  label?: string;
  type?: string;
  placeholder?: string;
  error?: string;
  disabled?: boolean;
  required?: boolean;
  id?: string;
}

const props = withDefaults(defineProps<Props>(), {
  type: 'text',
  placeholder: '',
  disabled: false,
  required: false,
  id: () => 'input-' + Math.random().toString(36).substring(2, 9)
});

const emit = defineEmits<{
  (e: 'update:modelValue', value: string): void;
}>();

const handleInput = (event: Event) => {
  const target = event.target as HTMLInputElement;
  emit('update:modelValue', target.value);
};

const inputBorderClass = computed(() => {
  if (props.error) {
    return 'border-destructive focus-visible:ring-destructive';
  }
  return 'border-border focus-visible:ring-primary focus-visible:border-primary';
});
</script>

<template>
  <div class="w-full flex flex-col gap-1.5">
    <label v-if="label" :for="id" class="text-sm font-medium text-foreground select-none">
      {{ label }}
      <span v-if="required" class="text-destructive ml-0.5">*</span>
    </label>
    <div class="relative flex items-center">
      <input
        :id="id"
        :type="type"
        :value="modelValue"
        :placeholder="placeholder"
        :disabled="disabled"
        :required="required"
        @input="handleInput"
        :class="[
          'w-full h-10 px-3 py-2 bg-surface rounded-lg border text-sm text-foreground placeholder:text-muted-foreground/60 transition-colors',
          'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-offset-1 disabled:opacity-50 disabled:cursor-not-allowed',
          inputBorderClass
        ]"
      />
      <slot name="suffix" />
    </div>
    <span v-if="error" class="text-xs text-destructive mt-0.5 animate-fade-in font-medium">
      {{ error }}
    </span>
  </div>
</template>
