<script lang="ts">
	import GenericDialog from './GenericDialog.svelte';

	let dialog: HTMLDialogElement;
</script>

<div class="avatar" onclick={() => dialog.show()}>
	<div class="w-12 rounded-full">
		<img src="https://img.daisyui.com/images/profile/demo/yellingcat@192.webp" />
	</div>
</div>

<GenericDialog bind:dialog>
	<div>Hello</div>
</GenericDialog>
