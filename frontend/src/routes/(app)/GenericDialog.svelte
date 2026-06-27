<script lang="ts">
	import type { Snippet } from 'svelte';

	interface DialogProps {
		dialog: HTMLDialogElement;
		closeHandler: (() => void) | never;
		children: Snippet;
	}
	let {
		dialog = $bindable<HTMLDialogElement>(),
		closeHandler = () => {},
		children
	} = $props() as DialogProps;
</script>

<dialog bind:this={dialog} class="modal">
	<div class="modal-box">
		{@render children()}
	</div>
	<form method="dialog" class="modal-backdrop">
		<button
			onclick={() => {
				if (closeHandler) {
					closeHandler();
				}
			}}>close</button
		>
	</form>
</dialog>
