// Draft Manager for LinkBlog Admin
// Handles saving, loading, listing, and deleting drafts using browser localStorage

document.addEventListener('DOMContentLoaded', function() {
    initDraftManager();
});

function initDraftManager() {
    // Initialize the draft manager UI when DOM is loaded
    const draftToggle = document.getElementById('draft-toggle');
    const saveDraftBtn = document.getElementById('save-draft-btn');
    const draftNameInput = document.getElementById('draft-name');
    const draftContainer = document.getElementById('draft-container');
    const draftList = document.getElementById('draft-list');
    
    if (!draftToggle || !saveDraftBtn || !draftNameInput || !draftContainer || !draftList) {
        console.warn('Draft manager elements not found in the DOM');
        return;
    }
    
    // Toggle draft manager visibility
    draftToggle.addEventListener('click', function() {
        const isExpanded = draftContainer.classList.contains('expanded');
        if (isExpanded) {
            draftContainer.classList.remove('expanded');
            draftContainer.classList.add('hidden');
            draftToggle.textContent = '► Draft Manager';
        } else {
            draftContainer.classList.remove('hidden');
            draftContainer.classList.add('expanded');
            draftToggle.textContent = '▼ Draft Manager';
            // Refresh draft list when expanded
            refreshDraftList();
        }
    });
    
    // Save draft button
    saveDraftBtn.addEventListener('click', function() {
        const draftName = draftNameInput.value.trim();
        if (!draftName) {
            showDraftMessage('Draft name cannot be empty', 'error');
            return;
        }
        
        const formData = getCurrentFormData();
        if (saveDraft(draftName, formData)) {
            showDraftMessage(`Draft "${draftName}" saved successfully`, 'success');
            draftNameInput.value = '';
            refreshDraftList();
        } else {
            showDraftMessage('Failed to save draft', 'error');
        }
    });
    
    // Register form submit handler to clear drafts
    const postForm = document.querySelector('form[name="postForm"]');
    if (postForm) {
        postForm.addEventListener('submit', function() {
            // After successful form submission, we'll clear any draft with the same name
            // This happens in the browser before the form is submitted
            const currentDraft = draftNameInput.value.trim();
            if (currentDraft) {
                setTimeout(() => deleteDraft(currentDraft), 500);
            }
        });
    }
    
    // Initial draft list
    refreshDraftList();
}

// Constants
const DRAFT_PREFIX = 'linkblog_draft_';
const DRAFT_LIST_KEY = 'linkblog_draft_list';

// Save a draft to localStorage
function saveDraft(draftName, postData) {
    try {
        // Create a unique key for this draft
        const draftKey = DRAFT_PREFIX + draftName;
        
        // Store the draft data
        localStorage.setItem(draftKey, JSON.stringify(postData));
        
        // Update the draft list
        const draftList = getDraftList();
        if (!draftList.includes(draftName)) {
            draftList.push(draftName);
            localStorage.setItem(DRAFT_LIST_KEY, JSON.stringify(draftList));
        }
        
        return true;
    } catch (error) {
        console.error('Error saving draft:', error);
        return false;
    }
}

// Load a draft from localStorage
function loadDraft(draftName) {
    try {
        const draftKey = DRAFT_PREFIX + draftName;
        const draftData = localStorage.getItem(draftKey);
        
        if (draftData) {
            const data = JSON.parse(draftData);
            fillFormWithData(data);
            showDraftMessage(`Draft "${draftName}" loaded successfully`, 'success');
            return true;
        }
        showDraftMessage(`Could not find draft "${draftName}"`, 'error');
        return false;
    } catch (error) {
        console.error('Error loading draft:', error);
        showDraftMessage('Error loading draft', 'error');
        return false;
    }
}

// Get list of all available drafts
function getDraftList() {
    try {
        const draftListJson = localStorage.getItem(DRAFT_LIST_KEY);
        return draftListJson ? JSON.parse(draftListJson) : [];
    } catch (error) {
        console.error('Error getting draft list:', error);
        return [];
    }
}

// Delete a draft from localStorage
function deleteDraft(draftName) {
    try {
        const draftKey = DRAFT_PREFIX + draftName;
        
        // Remove the draft
        localStorage.removeItem(draftKey);
        
        // Update the draft list
        const draftList = getDraftList();
        const updatedList = draftList.filter(name => name !== draftName);
        localStorage.setItem(DRAFT_LIST_KEY, JSON.stringify(updatedList));
        
        showDraftMessage(`Draft "${draftName}" deleted successfully`, 'success');
        refreshDraftList();
        return true;
    } catch (error) {
        console.error('Error deleting draft:', error);
        showDraftMessage('Error deleting draft', 'error');
        return false;
    }
}

// Fill form fields with draft data
function fillFormWithData(data) {
    // Fill form fields
    document.getElementById('PostTitle').value = data.title || '';
    document.getElementById('ShortTitle').value = data.shortTitle || '';
    document.getElementById('PostLink').value = data.link || '';
    document.getElementById('LinkTitle').value = data.linkTitle || '';
    document.getElementById('PostTags').value = data.tags || '';
    
    // For Trix editor, we need to set both the hidden input and the editor content
    const contentInput = document.getElementById('PostContent');
    if (contentInput) {
        contentInput.value = data.contents || '';

        // Find the trix-editor element and set its content
        const trixEditor = document.querySelector('trix-editor');
        if (trixEditor && trixEditor.editor) {
            trixEditor.editor.loadHTML(data.contents || '');
        }
    }

    // Trigger change events to update form validation
    ['PostTitle', 'ShortTitle', 'PostContent', 'PostLink', 'LinkTitle', 'PostTags'].forEach(id => {
        const element = document.getElementById(id);
        if (element) {
            const event = new Event('change', { bubbles: true });
            element.dispatchEvent(event);
            const inputEvent = new Event('input', { bubbles: true });
            element.dispatchEvent(inputEvent);
        }
    });
}

// Get current form data
function getCurrentFormData() {
    return {
        title: document.getElementById('PostTitle')?.value || '',
        shortTitle: document.getElementById('ShortTitle')?.value || '',
        contents: document.getElementById('PostContent')?.value || '',
        link: document.getElementById('PostLink')?.value || '',
        linkTitle: document.getElementById('LinkTitle')?.value || '',
        tags: document.getElementById('PostTags')?.value || ''
    };
}

// Refresh the draft list display
function refreshDraftList() {
    const draftList = document.getElementById('draft-list');
    if (!draftList) return;

    // Clear current list
    draftList.innerHTML = '';

    // Get drafts
    const drafts = getDraftList();

    if (drafts.length === 0) {
        draftList.innerHTML = '<p>No drafts saved yet.</p>';
        return;
    }

    // Add each draft to the list
    drafts.forEach(draftName => {
        const draftItem = document.createElement('div');
        draftItem.className = 'draft-item';

        const draftTitle = document.createElement('span');
        draftTitle.className = 'draft-title';
        draftTitle.textContent = draftName;

        const actions = document.createElement('div');
        actions.className = 'draft-item-actions';

        const loadBtn = document.createElement('button');
        loadBtn.className = 'btn btn-secondary';
        loadBtn.textContent = 'Load';
        loadBtn.onclick = function() { loadDraft(draftName); };

        const deleteBtn = document.createElement('button');
        deleteBtn.className = 'btn btn-danger';
        deleteBtn.textContent = 'Delete';
        deleteBtn.onclick = function() { deleteDraft(draftName); };

        actions.appendChild(loadBtn);
        actions.appendChild(deleteBtn);

        draftItem.appendChild(draftTitle);
        draftItem.appendChild(actions);

        draftList.appendChild(draftItem);
    });
}

// Show draft message (success or error)
function showDraftMessage(message, type) {
    const messageDiv = document.getElementById('draft-message');
    if (!messageDiv) return;
    
    messageDiv.textContent = message;
    messageDiv.className = type === 'error' ? 'alert alert-danger' : 'alert alert-success';
    messageDiv.style.display = 'block';
    
    // Auto hide after 3 seconds
    setTimeout(() => {
        messageDiv.style.display = 'none';
    }, 3000);
}